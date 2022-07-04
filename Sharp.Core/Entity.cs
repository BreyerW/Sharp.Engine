using Sharp.Core;
using Sharp.Engine.Components;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;


namespace Sharp
{
	delegate bool MaskCheck(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags);
	public enum TestMode
	{
		Any,
		All,
		Cull
	}
	public sealed class Entity : IEngineObject, IJsonOnDeserialized
	{
		//TODO: consider doing custom structure specifically designed for bitmasks eg. bit trie or avl/red-black tree or other, max depth to the highest bit set (after that everything is implicitly 0),
		//and maybe try to optimize for repeating patterns of 0&1 by adding int skipbytes or resignate and implicitly treat right as 1 and left as 0
		//also use Unsafe.Add to branchlessly choose left or right and dont use array to avoid allocating 2 length arrays


		//or use specialized renderer and special behaviour lists?
		internal static Dictionary<(BitMask components, BitMask tags), HashSet<Entity>> tagsMapping = new();//key is bit position also make it for system parts where mask defines what components entity has at least once
		[JsonIgnore]
		public Entity parent;
		//public bool visible;//TODO: mark all objects as not visible every frame then when frustrum culling in bepuphysic or physx mark them as visible
		[JsonIgnore]
		public Transform transform;//TODO: remove it and make caching responsibility of user
		[JsonIgnore]
		public List<Entity> childs = new List<Entity>();
		public string name = "Entity Object";
		private BitMask tagsMask = new(0);

		private BitMask componentsMask = new(0);

		[JsonInclude]
		public BitMask ComponentsMask
		{
			get => componentsMask;
			private set
			{
				if (tagsMapping.TryGetValue((componentsMask, tagsMask), out var set))
					set.Remove(this);
				if (tagsMapping.TryGetValue((value, tagsMask), out set) is false)
				{
					set = new HashSet<Entity>();
					tagsMapping.Add((value, tagsMask), set);
				}
				set.Add(this);
				componentsMask = value;
			}
		}
		[JsonInclude]
		public BitMask TagsMask
		{
			get => tagsMask;
			set
			{
				if (tagsMapping.TryGetValue((componentsMask, tagsMask), out var set))
					set.Remove(this);
				if (tagsMapping.TryGetValue((componentsMask, value), out set) is false)
				{
					set = new HashSet<Entity>();
					tagsMapping.Add((value, tagsMask), set);
				}
				set.Add(this);
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
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWithTags(BitMask mask, TestMode testMode = TestMode.All)
		{
			if (testMode is TestMode.Cull)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.tags.HasNoFlags(mask))
						yield return value;
			}
			else if (testMode is TestMode.All)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.tags.HasAllFlags(mask))
						yield return value;
			}
			else
				foreach (var (key, value) in tagsMapping)
					if (key.tags.HasAnyFlags(mask))
						yield return value;
		}
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWithComponents(BitMask mask, TestMode testMode = TestMode.All)
		{
			if (testMode is TestMode.Cull)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasNoFlags(mask))
						yield return value;
			}
			else if (testMode is TestMode.All)
			{
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasAllFlags(mask))
						yield return value;
			}
			else
				foreach (var (key, value) in tagsMapping)
					if (key.components.HasAnyFlags(mask))
						yield return value;
		}
		public static IEnumerable<IReadOnlyCollection<Entity>> FindAllWith(BitMask componentsMask, BitMask tagsMask, TestMode componentsTestMode = TestMode.All, TestMode tagsTestMode = TestMode.All)
		{
			MaskCheck condition = (componentsTestMode, tagsTestMode) switch
			{
				(TestMode.All, TestMode.All) => compsAndTags,
				(TestMode.Cull, TestMode.Cull) => nCompsAndnTags,
				(TestMode.All, TestMode.Cull) => compsAndnTags,
				(TestMode.Cull, TestMode.All) => nCompsAndTags,

				(TestMode.Any, TestMode.Any) => anyCompsAndAnyTags,
				(TestMode.Any, TestMode.All) => anyCompsAndTags,
				(TestMode.Any, TestMode.Cull) => anyCompsAndnTags,
				(TestMode.Cull, TestMode.Any) => nCompsAndAnyTags,
				(TestMode.All, TestMode.Any) => compsAndAnyTags,
			};
			foreach (var (key, value) in tagsMapping)
				if (condition(key.components, key.tags, componentsMask, tagsMask))
					yield return value;
		}
		private static bool compsAndTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool nCompsAndnTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasNoFlags(tags);
		private static bool nCompsAndTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool compsAndnTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasNoFlags(tags);

		private static bool anyCompsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasAnyFlags(tags);
		private static bool anyCompsAndTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool anyCompsAndnTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasNoFlags(tags);
		private static bool nCompsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasAnyFlags(tags);
		private static bool compsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasAnyFlags(tags);

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
			//components.Add(comp);
			return comp;
		}

		public void Dispose()
		{
			foreach (var component in components)
				component.Dispose();
			foreach (var child in childs)
				child.Dispose();
			tagsMapping[(componentsMask, tagsMask)].Remove(this);
			Extension.entities.RemoveEngineObject(this);
			//PluginManager.serializer.objToIdMapping.Remove(this);
		}

		public override string ToString()
		{
			return name;
		}

		public void OnDeserialized()
		{
			throw new NotImplementedException();
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