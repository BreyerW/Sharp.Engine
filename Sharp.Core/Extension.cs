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
		private static ConditionalWeakTable<object, byte[]> objectToIdMapping = new ConditionalWeakTable<object, byte[]>();
		public static Guid GetInstanceID(this object obj)
		{

			if (!objectToIdMapping.TryGetValue(obj, out var id))
			{
				objectToIdMapping.Add(obj, id = Guid.NewGuid().ToByteArray());
			}
			return new Guid(id);
		}

		public static T GetInstanceObject<T>(in this Guid id) where T : class
		{
			entities.idToObjectMapping.TryGetValue(id, out var obj);
			return (T)obj;
		}
		public static IEngineObject GetInstanceObject(in this Guid id)
		{
			entities.idToObjectMapping.TryGetValue(id, out var obj);
			return obj;
		}
		internal static void AddRestoredObject(this IEngineObject obj, in Guid id)
		{
			objectToIdMapping.Add(obj, id.ToByteArray());
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
		internal List<Entity> root = new List<Entity>();
		internal Dictionary<Guid, IEngineObject> idToObjectMapping = new Dictionary<Guid, IEngineObject>();
		internal Action OnRenderFrame;

		internal void AddEngineObject(Entity entity)
		{
			if (entity.parent is null)
				root.Add(entity);
			idToObjectMapping.Add(entity.GetInstanceID(), entity);
			SceneView.onAddedEntity?.Invoke(entity);
		}

		internal void RemoveEngineObject(Entity entity)
		{
			if (entity.parent is null)
				root.Remove(entity);
			idToObjectMapping.Remove(entity.GetInstanceID());
			SceneView.onRemovedEntity?.Invoke(entity);
		}

		internal void AddEngineObject(IEngineObject obj)
		{
			idToObjectMapping.Add(obj.GetInstanceID(), obj);
			SceneView.onAddedEntity?.Invoke(obj);
			if (obj is IStartableComponent start)
				SceneView.startables.Enqueue(start);
		}

		internal void RemoveEngineObject(IEngineObject obj)
		{
			idToObjectMapping.Remove(obj.GetInstanceID());
			SceneView.onRemovedEntity?.Invoke(obj);
		}
		internal void AddRestoredEngineObject(Entity obj, in Guid id)
		{
			if (obj.parent is null)
				root.Add(obj);
			idToObjectMapping.Add(id, obj);
			SceneView.onAddedEntity?.Invoke(obj);
		}
		internal void AddRestoredEngineObject(IEngineObject obj, in Guid id)
		{
			idToObjectMapping.Add(id, obj);
			SceneView.onAddedEntity?.Invoke(obj);
			if (obj is IStartableComponent start)
				SceneView.startables.Enqueue(start);
		}
	}
}