using EricsLib;
using EricsLib.Engine;
using EricsLib.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricsLib
{
    public abstract class Physical : GenericGameObject, IRenderable
    {
        public enum PhysicalType
        {
            Unknown = 1,
            Terrain = 2,
            Unit = 4,
            Resource = 8,
            Item = 16,
            Projectile = 32,
            Ethereal = 64,      //stuff can go through this, not affected by most forces such as gravity
            ALL = Unknown | Terrain | Unit | Resource | Item | Projectile | Ethereal
        };

        /// <summary>
        /// This is the broad phase bounding sphere. Use this first for any collision detection since it is the fastest to calculate.
        /// Note: Use length squared to avoid a square root calculation!
        /// </summary>
        protected BoundingSphere m_boundingSphere;

        /// <summary>
        /// this is a coarse bounding box for the entire object. If this bounding box doesn't intersect with another coarse bounding box, 
        /// then there isn't a need for any further collision checks.
        /// </summary>
        protected BoundingBox m_boundingBox;

        protected Vector3 m_position;
        protected Vector3 m_lastPosition;
        protected Quaternion m_orientation;

        /// <summary>
        /// note that velocity may be distinct from orientation (ie. facing forwards but moving backwards)
        /// </summary>
        protected Vector3 m_velocity;
        protected Vector3 m_acceleration;
        protected Range m_velocityLimits;                    //you can put limits on how fast you can go
        protected PhysicalType m_type;

        protected Effect m_effect;

        /// <summary>
        /// This indicates that the object doesn't actually move (such as terrain)
        /// </summary>
        protected bool m_static = true;
        protected bool m_hasBounds = false;
        protected bool m_selected = false;
        protected int m_lod = 0;

        public Physical()
        {
            m_velocityLimits = new Range();
            m_type = PhysicalType.Unknown;
            m_position = Vector3.Zero;
            m_lastPosition = Vector3.Zero;
            m_orientation = Quaternion.Identity;
            m_velocity = Vector3.Zero;
            m_acceleration = Vector3.Zero;
            m_boundingSphere = new BoundingSphere();
            m_boundingBox = new BoundingBox();
        }

        public abstract void Initialize(GraphicsDevice gd);

        /// <summary>
        /// Moves an object according to its position, velocity and accelleration and the change in game time
        /// </summary>
        /// <param name="gameTime">The change in game time</param>
        /// <returns>True - the object was moved.
        /// False - The object did not move.</returns>
        public virtual bool Update(GameTime gameTime)
        {
            if (!m_static)
            {
                m_lastPosition = m_position;
                m_velocity += m_acceleration * (float)(gameTime.ElapsedGameTime.TotalSeconds);
                m_position += m_velocity * (float)(gameTime.ElapsedGameTime.TotalSeconds);
                m_boundingSphere.Center = m_position;

                return m_lastPosition != m_position;    //lets you know if the object actually moved relative to its last position
            }

            return false;
        }

        public abstract int Render(Camera3D currentCamera);
        public virtual void UpdateLOD(Camera3D currentCamera)
        {
            float dist = (currentCamera.Position - m_position).LengthSquared();

            if (dist <= 2500)
                m_lod = 0;
            else if (dist <= 10000)
                m_lod = 1;
            else
                m_lod = 2;

        }
        public abstract IntersectionRecord Intersects(Ray intersectionRay);

        public virtual Matrix Projection
        {
            set { m_effect.Parameters["xProjection"].SetValue(value); }
        }
        public virtual Matrix View
        {
            set { m_effect.Parameters["xView"].SetValue(value); }
        }

        public virtual void SetDirectionalLight(Vector3 direction, Color color)
        {
            m_effect.Parameters["xLightDirection0"].SetValue(direction);
            m_effect.Parameters["xLightColor0"].SetValue(color.ToVector3());
            m_effect.Parameters["xEnableLighting"].SetValue(true);
        }

        /// <summary>
        /// Tells you if the bounding regions for this object [intersect or are contained within] the bounding frustum
        /// </summary>
        /// <param name="intersectionFrustum">The frustum to do bounds checking against</param>
        /// <returns>An intersection record containing any intersection information, or null if there isn't any
        /// </returns>
        public virtual IntersectionRecord Intersects(BoundingFrustum intersectionFrustum)
        {
            if (m_boundingBox != null && m_boundingBox.Max - m_boundingBox.Min != Vector3.Zero)
            {
                if (intersectionFrustum.Contains(m_boundingBox) != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }
            else if (m_boundingSphere != null && m_boundingSphere.Radius != 0f)
            {
                if (intersectionFrustum.Contains(m_boundingSphere) != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }

            return null;
        }

        /// <summary>
        /// Coarse collision check: Tells you if this object intersects with the given intersection sphere.
        /// </summary>
        /// <param name="intersectionSphere">The intersection sphere to check against</param>
        /// <returns>An intersection record containing this object</returns>
        /// <remarks>You'll want to override this for granular collision detection</remarks>
        public virtual IntersectionRecord Intersects(BoundingSphere intersectionSphere)
        {
            if (m_boundingBox != null && m_boundingBox.Max != m_boundingBox.Min)
            {
                if (m_boundingBox.Contains(intersectionSphere) != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }
            else if (m_boundingSphere != null && m_boundingSphere.Radius != 0f)
            {
                if (m_boundingSphere.Contains(intersectionSphere) != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }

            return null;
        }

        /// <summary>
        /// Coarse collision check: Tells you if this object intersects with the given intersection box.
        /// </summary>
        /// <param name="intersectionBox">The intersection box to check against</param>
        /// <returns>An intersection record containing this object</returns>
        /// <remarks>You'll want to override this for granular collision detection</remarks>
        public virtual IntersectionRecord Intersects(BoundingBox intersectionBox)
        {
            if (m_boundingBox != null && m_boundingBox.Max != m_boundingBox.Min)
            {
                ContainmentType ct = m_boundingBox.Contains(intersectionBox);
                if (ct != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }
            else if (m_boundingSphere != null && m_boundingSphere.Radius != 0f)
            {
                if (m_boundingSphere.Contains(intersectionBox) != ContainmentType.Disjoint)
                    return new IntersectionRecord(this);
            }

            return null;
        }

        /// <summary>
        /// Tests for intersection with this object against the other object
        /// </summary>
        /// <param name="otherObj">The other object to test for intersection against</param>
        /// <returns>Null if there isn't an intersection, an intersection record if there is a hit.</returns>
        public virtual IntersectionRecord Intersects(Physical otherObj)
        {
            IntersectionRecord ir;

            if (otherObj.m_boundingBox != null && otherObj.m_boundingBox.Min != otherObj.m_boundingBox.Max)
            {
                ir = Intersects(otherObj.m_boundingBox);
            }
            else if (otherObj.m_boundingSphere != null && otherObj.m_boundingSphere.Radius != 0f)
            {
                ir = Intersects(otherObj.m_boundingSphere);
            }
            else
                return null;

            if (ir != null)
            {
                ir.PhysicalObject = this;
                ir.OtherPhysicalObject = otherObj;
            }

            return ir;
        }

        public virtual void HandleIntersection(IntersectionRecord ir)
        {

        }

        #region helper functions
        public void UndoLastMove()
        {
            m_position = m_lastPosition;
        }

        public void SetCollisionRadius(float radius)
        {
            m_boundingSphere.Radius = radius;
        }
        #endregion

        #region Accessors
        public PhysicalType Type { get { return m_type; } }

        public Vector3 Position
        {
            get
            {
                return m_position;
            }
            set
            {
                m_position = value;
                m_boundingSphere.Center = value;
                //what about objects which have a bounding box?!
            }
        }

        /// <summary>
        /// Indicates whether or not this object has been selected by a player.
        /// </summary>
        public bool Selected
        {
            get { return m_selected; }
            set { m_selected = value; }
        }

        public bool IsStatic
        {
            get { return m_static; }
            set { m_static = value; }
        }

        public BoundingBox BoundingBox
        {
            get
            {
                return m_boundingBox;
            }
            set
            {
                m_boundingBox = value;
            }
        }

        public BoundingSphere BoundingSphere
        {
            get
            {
                return m_boundingSphere;
            }
            set
            {
                m_boundingSphere = value;
            }
        }

        public Quaternion Orientation
        {
            get
            {
                return m_orientation;
            }
            set
            {
                m_orientation = value;
            }
        }

        public Range SpeedLimit
        {
            get
            {
                return SpeedLimit;
            }
            set
            {
                SpeedLimit = value;
            }
        }

        public Vector3 Velocity
        {
            get
            {
                return m_velocity;
            }
            set
            {
                m_velocity = value;
            }
        }

        public Vector3 Accelleration
        {
            get { return m_acceleration; }
            set { m_acceleration = value; }
        }

        public float SpeedSquared
        {
            get { return m_velocity.LengthSquared(); }
        }

        public float Speed
        {
            get { return m_velocity.Length(); }
            set
            {
                m_velocity.Normalize();
                m_velocity *= value;
            }
        }

        /// <summary>
        /// tells you if a valid bounding area encloses the object. Doesn't indicate which kind though.
        /// </summary>
        public bool HasBounds
        {
            get
            {
                return (m_boundingSphere.Radius != 0 || m_boundingBox.Min != m_boundingBox.Max);
            }
        }

        public Effect Effect
        {
            get { return m_effect; }
            set { m_effect = value; }
        }

        #endregion
    }
}
