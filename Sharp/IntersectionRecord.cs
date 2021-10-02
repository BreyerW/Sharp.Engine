using EricsLib.Geometries;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EricsLib
{
    public class IntersectionRecord
    {

        Vector3 m_position;
        /// <summary>
        /// This is the exact point in 3D space which has an intersection.
        /// </summary>
        public Vector3 Position { get { return m_position; } }


        Vector3 m_normal;
        /// <summary>
        /// This is the normal of the surface at the point of intersection
        /// </summary>
        public Vector3 Normal { get { return m_normal; } }

        Ray m_ray;
        /// <summary>
        /// This is the ray which caused the intersection
        /// </summary>
        public Ray Ray { get { return m_ray; } }

        Physical m_intersectedObject1;
        /// <summary>
        /// This is the object which is being intersected
        /// </summary>
        public Physical PhysicalObject
        {
            get { return m_intersectedObject1; }
            set { m_intersectedObject1 = value; }
        }

        Physical m_intersectedObject2;
        /// <summary>
        /// This is the other object being intersected (may be null, as in the case of a ray-object intersection)
        /// </summary>
        public Physical OtherPhysicalObject
        {
            get { return m_intersectedObject2; }
            set { m_intersectedObject2 = value; }
        }

        /// <summary>
        /// this is a reference to the current node within the octree for where the collision occurred. In some cases, the collision handler
        /// will want to be able to spawn new objects and insert them into the tree. This node is a good starting place for inserting these objects
        /// since it is a very near approximation to where we want to be in the tree.
        /// </summary>
        OctTree m_treeNode;

        /// <summary>
        /// check the object identities between the two intersection records. If they match in either order, we have a duplicate.
        /// </summary>
        /// <param name="otherRecord">the other record to compare against</param>
        /// <returns>true if the records are an intersection for the same pair of objects, false otherwise.</returns>
        public override bool Equals(object otherRecord)
        {
            IntersectionRecord o = (IntersectionRecord)otherRecord;
            //
            //return (m_intersectedObject1 != null && m_intersectedObject2 != null && m_intersectedObject1.ID == m_intersectedObject2.ID);
            if (otherRecord == null)
                return false;
            if (o.m_intersectedObject1.ID == m_intersectedObject1.ID && o.m_intersectedObject2.ID == m_intersectedObject2.ID)
                return true;
            if (o.m_intersectedObject1.ID == m_intersectedObject2.ID && o.m_intersectedObject2.ID == m_intersectedObject1.ID)
                return true;
            return false;


        }


        double m_distance;
        /// <summary>
        /// This is the distance from the ray to the intersection point. 
        /// You'll usually want to use the nearest collision point if you get multiple intersections.
        /// </summary>
        public double Distance { get { return m_distance; } }

        private bool m_hasHit = false;
        public bool HasHit
        {
            get { return m_hasHit; }
        }

        public IntersectionRecord()
        {
            m_position = Vector3.Zero;
            m_normal = Vector3.Zero;
            m_ray = new Ray();
            m_distance = float.MaxValue;
            m_intersectedObject1 = null;
        }

        public IntersectionRecord(Vector3 hitPos, Vector3 hitNormal, Ray ray, double distance)
        {
            m_position = hitPos;
            m_normal = hitNormal;
            m_ray = ray;
            m_distance = distance;
            //m_hitObject = hitGeom;
            m_hasHit = true;
        }

        /// <summary>
        /// Creates a new intersection record indicating whether there was a hit or not and the object which was hit.
        /// </summary>
        /// <param name="hitObject">Optional: The object which was hit. Defaults to null.</param>
        public IntersectionRecord(Physical hitObject = null)
        {
            m_hasHit = hitObject != null;
            m_intersectedObject1 = hitObject;
            m_position = Vector3.Zero;
            m_normal = Vector3.Zero;
            m_ray = new Ray();
            m_distance = 0.0f;
        }
    }
}
