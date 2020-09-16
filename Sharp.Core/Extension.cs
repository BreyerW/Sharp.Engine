using System;
using System.Threading;
using Sharp.Editor.Views;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Engine.Components;

namespace Sharp
{
	public static class Extension
	{
		internal static Root entities = new Root();
		//TODO: delete implicit id someday if someone really want to keep track of cyclic references and doesnt own source code
		//they can wrap - or extend if not sealed - said class and implement IEngineObject
		internal static Dictionary<object, Guid> objectToIdMapping = new Dictionary<object, Guid>();//TODO: remove on destroy?
		public static Guid GetInstanceID<T>(this T obj) where T : class
		{
			if (!objectToIdMapping.TryGetValue(obj, out var id))
			{
				objectToIdMapping.Add(obj, id = Guid.NewGuid());
				//throw new InvalidOperationException("attempted to add new entity this shouldnt be happening");
			}
			return id;
		}

		public static T GetInstanceObject<T>(in this Guid id) where T : class
		{
			Root.idToObjectMapping.TryGetValue(id, out var obj);
			return (T)obj;
		}
		public static IEngineObject GetInstanceObject(in this Guid id)
		{
			Root.idToObjectMapping.TryGetValue(id, out var obj);
			return obj;
		}
		internal static void AddRestoredObject<T>(this T obj, in Guid id) where T : class
		{
			objectToIdMapping.Add(obj, id);
		}
		public static TKey GetKey<TKey, TValue>(this Dictionary<TKey, TValue> dict, TValue val)
		{
			foreach (var pair in dict)
				if (pair.Value.Equals(val))
					return pair.Key;
			return default;
		}
		public static ref T ReadAs<T>(this in IntPtr ptr) where T : unmanaged
		{
			unsafe
			{
				ref var addr = ref Unsafe.AddByteOffset(ref Unsafe.AsRef<byte>(ptr.ToPointer()), (IntPtr)IntPtr.Size);
				return ref Unsafe.As<byte, T>(ref addr);
			}
		}
		public static ref T ReadAs<T>(this in IntPtr ptr, out IntPtr lengthInBytes) where T : unmanaged
		{
			unsafe
			{
				lengthInBytes = Unsafe.AsRef<IntPtr>(ptr.ToPointer());
				ref var addr = ref Unsafe.AddByteOffset(ref Unsafe.AsRef<byte>(ptr.ToPointer()), (IntPtr)IntPtr.Size);
				return ref Unsafe.As<byte, T>(ref addr);
			}
		}
		public static int FindIndex<T>(this IEnumerable<T> items, Func<T, bool> predicate)
		{
			int index = 0;
			foreach (var item in items)
			{
				if (predicate(item)) return index;
				index++;
			}
			return -1;
		}
	}

	public static class Utils
	{
		public static void Swap<T>(ref T v1, ref T v2) where T : class
		{
			var prev = Interlocked.Exchange(ref v1, v2);
			Interlocked.Exchange(ref v2, prev);
		}
	}
	[Serializable]
	public class Root
	{
		public static HashSet<IEngineObject> addedEntities = new HashSet<IEngineObject>();
		public static HashSet<IEngineObject> removedEntities = new HashSet<IEngineObject>();
		internal static List<Entity> root = new List<Entity>();
		internal static Dictionary<Guid, IEngineObject> idToObjectMapping = new Dictionary<Guid, IEngineObject>();
		internal List<Renderer> renderers = new List<Renderer>();

		internal void AddEngineObject(IEngineObject obj)
		{
			idToObjectMapping.Add(obj.GetInstanceID(), obj);
			addedEntities.Add(obj);
			if (obj is Entity e && e.parent is null)
			{
				root.Add(e);
				return;
			}
			if (obj is IStartableComponent start)
				SceneView.startables.Enqueue(start);
			if (obj is Renderer r)
				renderers.Add(r);
		}

		internal void RemoveEngineObject(IEngineObject obj)
		{
			if (obj is Entity e && e.parent is null)
				root.Remove(e);
			removedEntities.Add(obj);
			var id = obj.GetInstanceID();
			idToObjectMapping.Remove(id);
			if (obj is Renderer r)
				renderers.Remove(r);
		}
		internal void AddRestoredEngineObject(IEngineObject obj, in Guid id)
		{
			idToObjectMapping.Add(id, obj);
			addedEntities.Add(obj);
			if (obj is Entity e && e.parent is null)
			{
				root.Add(e);
				return;
			}
			if (obj is IStartableComponent start)
				SceneView.startables.Enqueue(start);
			if (obj is Renderer r)
				renderers.Add(r);
		}
	}
}