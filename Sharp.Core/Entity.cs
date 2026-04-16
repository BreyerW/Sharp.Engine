using Sharp.Core;
using Sharp.Engine.Components;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;


namespace Sharp
{
	//we are making this struct because we expect most entities to have only one component of each type and we want to avoid allocating lists for them,
	//but we also want to support multiple components of the same type without allocating a list until necessary
	struct OneOrMany<T, TArray> where TArray : IList<T>, new()
	{
		[JsonInclude]
		private object oneOrMany;

		public Type UnderlyingType => (IsSingle ? oneOrMany : ((TArray)oneOrMany)[0]).GetType();
		public bool IsSingle => oneOrMany is not TArray;

		public int Count => IsSingle ? 1 : ((TArray)oneOrMany).Count;

		public OneOrMany(object obj)
		{
			oneOrMany = obj;
		}
		public void Add(T obj)
		{
			if (oneOrMany is null)
			{
				oneOrMany = obj;
			}
			else if (oneOrMany is TArray list)
			{
				list.Add(obj);
			}
			else
			{
				var newList = new TArray { (T)oneOrMany, obj };
				oneOrMany = newList;
			}
		}
		public T Get(int index = 0)
		{
			if (IsSingle)
			{
				return (T)oneOrMany;
			}
			else
			{
				var list = (TArray)oneOrMany;
				if (index < 0 || index >= list.Count)
					return default;
				return list[index];
			}
		}
	}
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
																											//[JsonIgnore]
		public Entity parent;
		//public bool visible;//TODO: mark all objects as not visible every frame then when frustrum culling in bepuphysic or physx mark them as visible
		//[JsonIgnore]
		public Transform transform;//TODO: remove it and make caching responsibility of user
								   //[JsonIgnore]
		public List<Entity> childs = new List<Entity>();
		public string name = "Entity Object";
		private BitMask tagsMask = new(0);

		private BitMask componentsMask = new(0);
		//allow swappoing to frozen dictionary when not adding new component types to save memory and improve lookup performance, but we need to make sure to swap back to normal dictionary when adding new component types?
		private IDictionary<Type, int> typeToIndexMapping = new Dictionary<Type, int>();

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
		[JsonInclude]
		internal List<OneOrMany<Component, List<Component>>> components = new();

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
				(TestMode.All, TestMode.All) => allCompsAndTags,
				(TestMode.Cull, TestMode.Cull) => noCompsAndNoTags,
				(TestMode.All, TestMode.Cull) => allCompsAndNoTags,
				(TestMode.Cull, TestMode.All) => noCompsAndAllTags,

				(TestMode.Any, TestMode.Any) => anyCompsAndAnyTags,
				(TestMode.Any, TestMode.All) => anyCompsAndAllTags,
				(TestMode.Any, TestMode.Cull) => anyCompsAndNoTags,
				(TestMode.Cull, TestMode.Any) => noCompsAndAnyTags,
				(TestMode.All, TestMode.Any) => allCompsAndAnyTags,
			};
			foreach (var (key, value) in tagsMapping)
				if (condition(key.components, key.tags, componentsMask, tagsMask))
					yield return value;
		}
		private static bool allCompsAndTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool noCompsAndNoTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasNoFlags(tags);
		private static bool noCompsAndAllTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool allCompsAndNoTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasNoFlags(tags);

		private static bool anyCompsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasAnyFlags(tags);
		private static bool anyCompsAndAllTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasAllFlags(tags);
		private static bool anyCompsAndNoTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAnyFlags(components) && tagsMask.HasNoFlags(tags);
		private static bool noCompsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasNoFlags(components) && tagsMask.HasAnyFlags(tags);
		private static bool allCompsAndAnyTags(in BitMask componentsMask, in BitMask tagsMask, in BitMask components, in BitMask tags) => componentsMask.HasAllFlags(components) && tagsMask.HasAnyFlags(tags);

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

		public T GetComponentExact<T>(int index = 0) where T : Component
		{
			return (T)GetComponentExact(typeof(T), index);
		}

		public Component GetComponentExact(Type type, int index = 0)
		{
			foreach (var component in components)
			{
				if (component.UnderlyingType == type )
					return component.Get(index);
			}
			return null;
		}
		public T GetComponent<T>(int index = 0) where T : Component
		{
			return (T)GetComponent(typeof(T), index);
		}

		public Component GetComponent(Type type, int index = 0)
		{
			foreach (var component in components)
			{
				if (component.UnderlyingType.IsAssignableTo(type))
					return component.Get(index);
			}
			return null;
		}
		public IEnumerable<Component> GetAllComponents()
		{
			foreach (var component in components)
			{
				for (int i = 0; i < component.Count; i++)
					yield return component.Get(i);
			}
		}
		public IEnumerable<T> GetAllComponents<T>() where T : Component
		{
			var type = typeof(T);
			foreach (var component in components)
			{
				if (component.UnderlyingType.IsAssignableTo(type))
				{
					for (int i = 0; i < component.Count; i++)
						yield return (T)component.Get(i);
				}
			}
		}
		public IEnumerable<T> GetAllComponentsExact<T>() where T : Component
		{
			var type = typeof(T);
			foreach (var component in components)
			{
				if (component.UnderlyingType.IsAssignableTo(type))
				{
					for (int i = 0; i < component.Count; i++)
						yield return (T)component.Get(i);
				}
			}
		}
		public T AddComponent<T>() where T : Component
		{
			var comp = Activator.CreateInstance<T>(); //RuntimeHelpers.GetUninitializedObject(typeof(T)) as T;
			comp.Parent = this;
			comp.active = true;
			comp.InternalInitialize();
			var type = comp.GetType();
			ComponentsMask = ComponentsMask.SetTag<T>();
			if (typeToIndexMapping.TryGetValue(type, out var index))
			{
				components[index].Add(comp);

			}
			else
			{
				typeToIndexMapping.Add(type, components.Count);
				components.Add(new OneOrMany<Component, List<Component>>(comp));
			}
			//if (comp is Transform t)
			//transform = t;

			return comp;
		}
		public void AddComponent(Component component)
		{
			component.Parent = this;
			component.active = true;
			component.InternalInitialize();
			var type = component.GetType();
			ComponentsMask = ComponentsMask.SetTag(type.Name);
			foreach (var comp in components)
			{
				if (comp.UnderlyingType == type)
				{
					comp.Add(component);
					return;
				}
			}
		}
		internal void AddComponentInternal(Component component)
		{
			var type = component.GetType();
			ComponentsMask = ComponentsMask.SetTag(type.Name);
			foreach (var comp in components)
			{
				if (comp.UnderlyingType == type)
				{
					comp.Add(component);
					return;
				}
			}
		}
		public void Dispose()
		{
			//foreach (var component in components)
			//component.Dispose();
			//foreach (var child in childs)
			//child.Dispose();
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