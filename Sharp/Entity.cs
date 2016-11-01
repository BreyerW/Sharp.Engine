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
        private Vector3 position = Vector3.Zero;

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

        private Vector3 rotation = Vector3.Zero;

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

        private Vector3 scale = Vector3.One;

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
            ModelMatrix = Matrix4.CreateScale(scale) * Matrix4.CreateFromQuaternion(ToQuaterion(rotation)) * Matrix4.CreateTranslation(position);
        }
        public Quaternion ToQuaterion(Vector3 angles)
        {
            // Assuming the angles are in radians.
            angles *= MathHelper.Pi / 180f;

            return Quaternion.FromMatrix(Matrix3.CreateRotationX(angles.X) * Matrix3.CreateRotationY(angles.Y) * Matrix3.CreateRotationZ(angles.Z));
        }
        public Vector3 ToEuler(Quaternion q)
        {
            float sqw = q.W * q.W;
            float sqx = q.X * q.X;
            float sqy = q.Y * q.Y;
            float sqz = q.Z * q.Z;
            float unit = sqx + sqy + sqz + sqw; // if normalised is one, otherwise is correction factor
            float test = q.X * q.Y + q.Z * q.W;
            if (test > 0.499 * unit)
            { // singularity at north pole
                return new Vector3(2f * (float)Math.Atan2(q.X, q.W), MathHelper.Pi / 2, 0) * 180f / MathHelper.Pi;
            }
            if (test < -0.499 * unit)
            { // singularity at south pole
                return new Vector3(-2f * (float)Math.Atan2(q.X, q.W), -MathHelper.Pi / 2, 0) * 180f / MathHelper.Pi;
            }
            return new Vector3((float)Math.Atan2(2 * q.Y * q.W - 2 * q.X * q.Z, sqx - sqy - sqz + sqw),
                (float)Math.Asin(2 * test / unit),
                (float)Math.Atan2(2 * q.X * q.W - 2 * q.Y * q.Z, -sqx + sqy - sqz + sqw)) * 180f / MathHelper.Pi;

        }
        public T GetComponent<T>() where T : Component
        {
            return components.OfType<T>().First();
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

