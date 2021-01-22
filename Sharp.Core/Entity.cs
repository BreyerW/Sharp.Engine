using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Collections.Extensions;
using Newtonsoft.Json;
using Sharp.Core;
using Sharp.Engine.Components;

namespace Sharp
{
	[Serializable, JsonObject]
	public sealed class Entity : IEngineObject
	{
		internal static MultiValueDictionary<(BitMask components, BitMask tags), Entity> tagsMapping = new();//key is bit position also make it for system parts where mask defines what components entity has at least once
		[JsonIgnore]
		public Entity parent;
		public bool visible;//TODO: mark all objects as not visible every frame then when frustrum culling in bepuphysic or physx mark them as visible
		[JsonIgnore]
		public Transform transform;//TODO: remove it and make caching responsibility of user
		[JsonIgnore]
		public List<Entity> childs = new List<Entity>();
		public string name = "Entity Object";
		private BitMask tagsMask = new(0);

		private BitMask componentsMask = new(0);
		[JsonProperty]
		public BitMask ComponentsMask
		{
			get => componentsMask;
			private set
			{
				tagsMapping.Remove((componentsMask, tagsMask), this);
				tagsMapping.Add((value, tagsMask), this);
				componentsMask = value;
			}
		}
		[JsonProperty]
		public BitMask TagsMask
		{
			get => tagsMask;
			set
			{
				tagsMapping.Remove((componentsMask, tagsMask), this);
				tagsMapping.Add((componentsMask, value), this);
				tagsMask = value;
			}
		}
		[JsonIgnore]
		internal List<Component> components = new List<Component>();

		public Entity()
		{
			Extension.entities.AddEngineObject(this);
			AddComponent<Transform>();
		}

		public Quaternion ToQuaterion(Vector3 angles)
		{
			// Assuming the angles are in radians.
			angles *= NumericsExtensions.Deg2Rad;

			return Quaternion.CreateFromRotationMatrix(Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z));
		}
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWithTags(BitMask mask, bool cull = false)
		{
			if (cull)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.tags.HasNoFlags(mask))
						yield return value;
			}
			else
				foreach (var (key, value) in tagsMapping)
					if (key.tags.HasFlags(mask))
						yield return value;

		}
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWithComponents(BitMask mask, bool cull = false)
		{
			if (cull)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasNoFlags(mask))
						yield return value;
			}
			else
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasFlags(mask))
						yield return value;

		}
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWithComponentsAndTags(BitMask componentsMask, BitMask tagsMask, bool cullComponents = false, bool cullTags = false)
		{
			if (cullComponents && cullTags)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasNoFlags(componentsMask) && key.tags.HasNoFlags(tagsMask))
						yield return value;
			}
			else if (cullComponents is false && cullTags is false)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasFlags(componentsMask) && key.tags.HasFlags(tagsMask))
						yield return value;
			}
			else if (cullComponents is false && cullTags)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasFlags(componentsMask) && key.tags.HasNoFlags(tagsMask))
						yield return value;
			}
			else
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasNoFlags(componentsMask) && key.tags.HasFlags(tagsMask))
						yield return value;
			}
		}
		public static Vector3 rotationMatrixToEulerAngles(Matrix4x4 mat)
		{
			//assert(isRotationMatrix(R));
			mat = Matrix4x4.Transpose(mat);
			float sy = (float)Math.Sqrt(mat.M32 * mat.M32 + mat.M33 * mat.M33);

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
			if (x is -0)
				x = 0;
			if (y is -0)
				y = 0;
			if (z is -0)
				z = 0;
			return new Vector3(x, y, z);
		}

		public T GetComponent<T>() where T : Component
		{
			return components.Find((obj) => obj is T) as T;
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
		public List<T> GetAllComponents<T>() where T : Component
		{
			return components.FindAll((obj) => obj is T).ConvertAll(comp => comp as T); //as List<T>;
		}
		public T AddComponent<T>() where T : Component
		{
			return AddComponent(typeof(T)) as T;
		}

		public Component AddComponent(Type type)
		{
			var comp = Activator.CreateInstance(type, this) as Component;
			comp.active = true;
			//if (comp is Transform t)
			//transform = t;
			ComponentsMask = ComponentsMask.SetTag(comp);
			components.Add(comp);
			return comp;
		}

		public void Dispose()
		{
			foreach (var component in components)
				component.Dispose();
			foreach (var child in childs)
				child.Dispose();
			tagsMapping.Remove((componentsMask, tagsMask), this);
			Extension.entities.RemoveEngineObject(this);
			Extension.objectToIdMapping.Remove(this);
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