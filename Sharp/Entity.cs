using System;
using System.Collections.Generic;
using OpenTK;
using Sharp.Editor.Views;
using System.Linq;

namespace Sharp
{
    public class Entity
    {
        private static int lastId = 0;
        internal Vector3 position = Vector3.Zero;

        public readonly int id;
        public Entity parent;
        public List<Entity> childs = new List<Entity>();
        public string name = "Entity Object";

        public Vector3 Position
        {
            get
            {
                return position;
            }
            set
            {
                position = value;
                OnTransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal Vector3 rotation = Vector3.Zero;

        public Vector3 Rotation
        {
            get
            {
                return rotation;
            }
            set
            {
                rotation = value;
                OnTransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        internal Vector3 scale = Vector3.One;

        public Vector3 Scale
        {
            get
            {
                return scale;
            }
            set
            {
                scale = value;
                OnTransformChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool active
        {
            get { return enabled; }
            set
            {
                if (enabled == value)
                    return;
                enabled = value;
                //if (enabled)
                //OnEnableInternal ();
                //else
                //OnDisableInternal ();
            }
        }

        private bool enabled;
        private HashSet<int> tags = new HashSet<int>();

        //public
        private Matrix4 modelMatrix;

        public ref Matrix4 ModelMatrix
        {
            get { return ref modelMatrix; }
        }

        public EventHandler OnTransformChanged;

        private List<Component> components = new List<Component>();

        public Entity()
        {
            OnTransformChanged += ((sender, e) => SetModelMatrix());
            id = ++lastId;
            lastId = id;
        }

        public static Entity[] FindAllWithTags(bool activeOnly = true, params string[] lookupTags)
        {
            //find all entities with tags in scene
            HashSet<Entity> intersectedArr;
            var id = TagsContainer.allTags.IndexOf(lookupTags[0]);
            intersectedArr = TagsContainer.entitiesToTag[id];
            foreach (var tag in lookupTags)
            {
                id = TagsContainer.allTags.IndexOf(tag);
                intersectedArr.IntersectWith(TagsContainer.entitiesToTag[id]);
            }
            if (activeOnly)
                foreach (var entity in intersectedArr)
                    if (!entity.active)
                        intersectedArr.Remove(entity);
            return intersectedArr.ToArray();
        }

        //public Entity[] FindWithTags(bool activeOnly=true, params string[] lookupTags){
        //find children entities with tags in scene
        //}
        public void AddTags(params string[] tagsToAdd)
        {
            foreach (var tag in tagsToAdd)
            {
                if (TagsContainer.allTags.Contains(tag))
                {
                    var id = TagsContainer.allTags.IndexOf(tag);
                    TagsContainer.entitiesToTag[id].Add(this);
                    tags.Add(id);
                }
                else
                {
                    throw new ArgumentException("Tag " + tag + " doesn't exist!");
                    //TagsContainer.allTags.Add (tag);
                    //TagsContainer.entitiesToTag.Add(new HashSet<Entity>(){this});
                }
            }
        }

        public void RemoveTags(params string[] tagsToRemove)
        {
            foreach (var tag in tagsToRemove)
            {
                var id = TagsContainer.allTags.IndexOf(tag);
                TagsContainer.entitiesToTag[id].Remove(this);
                tags.Remove(id);
            }
        }

        public void SetModelMatrix()
        {
            var angles = rotation * MathHelper.Pi / 180f;
            ModelMatrix = Matrix4.CreateScale(scale) * Matrix4.CreateRotationX(angles.X) * Matrix4.CreateRotationY(angles.Y) * Matrix4.CreateRotationZ(angles.Z) * Matrix4.CreateTranslation(position);
        }

        public Quaternion ToQuaterion(Vector3 angles)
        {
            // Assuming the angles are in radians.
            angles *= MathHelper.Pi / 180f;

            return Quaternion.FromMatrix(Matrix3.CreateRotationX(angles.X) * Matrix3.CreateRotationY(angles.Y) * Matrix3.CreateRotationZ(angles.Z));
        }

        public static Vector3 ToEuler(Quaternion q)
        {
            // Store the Euler angles in radians
            Vector3 pitchYawRoll = new Vector3();

            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;

            // If quaternion is normalised the unit is one, otherwise it is the correction factor
            double unit = sqx + sqy + sqz + sqw;
            double test = q.X * q.Y + q.Z * q.W;

            if (test > 0.4999f * unit)                              // 0.4999f OR 0.5f - EPSILON
            {
                // Singularity at north pole
                pitchYawRoll.Z = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
                pitchYawRoll.Y = MathHelper.Pi * 0.5f;                         // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else if (test < -0.4999f * unit)                        // -0.4999f OR -0.5f + EPSILON
            {
                // Singularity at south pole
                pitchYawRoll.Z = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
                pitchYawRoll.Y = -MathHelper.Pi * 0.5f;                        // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else
            {
                pitchYawRoll.Z = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);       // Yaw
                pitchYawRoll.Y = (float)Math.Asin(2f * test / unit);                                             // Pitch
                pitchYawRoll.X = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);      // Roll
            }

            return pitchYawRoll;
        }

        public static Vector3 rotationMatrixToEulerAngles(Matrix4 mat)
        {
            //assert(isRotationMatrix(R));
            mat.Transpose();
            float sy = (float)Math.Sqrt(mat[0, 0] * mat[0, 0] + mat[1, 0] * mat[1, 0]);

            bool singular = sy < 1e-6; // If

            float x, y, z;
            if (!singular)
            {
                x = (float)Math.Atan2(mat[2, 1], mat[2, 2]);
                y = (float)Math.Atan2(-mat[2, 0], sy);
                z = (float)Math.Atan2(mat[1, 0], mat[0, 0]);
            }
            else
            {
                x = (float)Math.Atan2(-mat[1, 2], mat[1, 1]);
                y = (float)Math.Atan2(-mat[2, 0], sy);
                z = 0;
            }
            return new Vector3(x, y, z);
        }

        public T GetComponent<T>() where T : Component
        {
            return (components.Find((obj) => obj is T)) as T;
        }

        public Component GetComponent(Type type)
        {
            foreach (var component in components)
                if (component.GetType().IsGenericType && component.GetType().GetGenericTypeDefinition() == type || component.GetType() == type)
                    return component;
            return null;
        }

        public List<Component> GetAllComponents()
        {
            return components;
        }

        public T AddComponent<T>() where T : Component, new()
        {
            return AddComponent(new T()) as T;
        }

        public Component AddComponent(Component comp)
        {
            comp.entityObject = this;
            components.Add(comp);
            return comp;
        }

        /*private Behaviour AddComponent (Behaviour comp)
		{
		//assign behaviour specific events to scene view
			components.Add (comp.GetType(),comp);
			return components [comp.GetType()] as Behaviour;
		}
		private Renderer AddComponent (Renderer comp)
		{
		//assign renderer specific events to scene view
			components.Add (comp.GetType(), comp);
			return components [comp.GetType()] as Renderer;
		}*/

        public void Instatiate()
        {
            SceneView.entities.Add(this);
            SceneView.OnAddedEntity?.Invoke();
            lastId = id;
        }

        public void Instatiate(Vector3 pos, Vector3 rot, Vector3 s)
        {
            scale = s;
            position = pos;
            rotation = rot;
            Instatiate();
        }

        public void Destroy()
        {
            SceneView.entities.Remove(this);
            SceneView.OnRemovedEntity?.Invoke();
        }

        public override string ToString()
        {
            return name;
        }
    }
}