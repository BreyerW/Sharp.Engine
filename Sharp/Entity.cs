﻿using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sharp.Engine.Components;

namespace Sharp
{
	[Serializable]
	public class Entity : IEngineObject
	{
		public Vector3 position = Vector3.Zero;
		internal Vector3 rotation = Vector3.Zero;
		internal Vector3 scale = Vector3.One;

		public Entity parent;
		public Transform transform;
		public List<Entity> childs = new List<Entity>();
		public string name = "Entity Object";

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

		private bool enabled = true;
		private HashSet<int> tags = new HashSet<int>();

		//public
		private Matrix4x4 modelMatrix;

		public ref readonly Matrix4x4 ModelMatrix
		{
			get { return ref modelMatrix; }
		}

		public Action onTransformChanged;

		private List<Component> components = new List<Component>();

		public Entity()
		{
			onTransformChanged += OnTransformChanged;
			transform = AddComponent<Transform>();
			SceneView.entities.AddEngineObject(this);
			//id = ++lastId;
			//lastId = id;
		}

		private void OnTransformChanged()
		{
			SetModelMatrix(); Console.WriteLine("transform changed");
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
			var angles = rotation * NumericsExtensions.Pi / 180f;
			modelMatrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
		}

		public Quaternion ToQuaterion(Vector3 angles)
		{
			// Assuming the angles are in radians.
			angles *= NumericsExtensions.Pi / 180f;

			return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z));
		}

		public static Vector3 rotationMatrixToEulerAngles(Matrix4x4 mat)
		{
			//assert(isRotationMatrix(R));
			mat = Matrix4x4.Transpose(mat);
			float sy = (float)Math.Sqrt(mat.M11 * mat.M11 + mat.M21 * mat.M21);

			bool singular = sy < 1e-6; // If

			float x, y, z;
			if (!singular)
			{
				x = (float)Math.Atan2(mat.M32, mat.M33);
				y = (float)Math.Atan2(-mat.M31, sy);
				z = (float)Math.Atan2(mat.M21, mat.M11);
			}
			else
			{
				x = (float)Math.Atan2(-mat.M23, mat.M22);
				y = (float)Math.Atan2(-mat.M31, sy);
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
			//if (comp is Renderer renderer)
			//    SceneView.renderers.Add(renderer);
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
			//lastId = id;
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
			SceneView.entities.RemoveEngineObject(this);
		}

		public override string ToString()
		{
			return name;
		}
	}

	[Serializable]
	public class SharpEvent<T>

	{
		private Action<T> action;

		internal SharpEvent(Action<T> action)

		{
			this.action = action;
		}
	}
}