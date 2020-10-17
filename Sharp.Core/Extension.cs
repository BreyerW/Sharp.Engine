using System;
using System.Threading;
using Sharp.Editor.Views;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Engine.Components;
using System.Reflection;
using System.Runtime.InteropServices;
using Sharp.Core;
using System.Runtime.InteropServices.ComTypes;
using System.Numerics;

namespace Sharp
{
	public static class Extension
	{
		private static Dictionary<string, int> flagToBitPositionMapping = new()//TODO: replace with generated enum?
		{
		};
		private static Dictionary<Type, int> compFlagToBitPositionMapping = new()
		{
			[typeof(Renderer)] = 0,
			[typeof(Behaviour)] = 1,
			[typeof(CommandBufferComponent)] = 2,
		};//TODO: replace with generated enum?

		public static ref readonly Bitask SetTag(this in Bitask mask, string tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);
			if (tag is "Nothing")
			{
				bitmask.ClearAll();
			}
			else if (tag is "Everything")
			{
				bitmask.SetAll();
			}
			else if (flagToBitPositionMapping.TryGetValue(tag, out var index))
				bitmask.SetFlag(index);
			else throw new ArgumentException($"{tag} does not exist.");

			return ref bitmask;
		}
		public static ref readonly Bitask ClearTag(this in Bitask mask, string tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);
			if (tag is "Nothing")
			{
				return ref bitmask;
			}
			else if (tag is "Everything")
			{
				bitmask.ClearAll();
			}
			else if (flagToBitPositionMapping.TryGetValue(tag, out var index))
				bitmask.ClearFlag(index);
			else throw new ArgumentException($"{tag} does not exist.");

			return ref bitmask;
		}
		public static ref readonly Bitask SetTag(this in Bitask mask, Component tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);
			if (tag is Renderer)
			{
				bitmask.SetFlag(compFlagToBitPositionMapping[typeof(Renderer)]);
			}
			else if (tag is CommandBufferComponent)
			{
				bitmask.SetFlag(compFlagToBitPositionMapping[typeof(CommandBufferComponent)]);
			}
			else if (tag is Behaviour)
			{
				bitmask.SetFlag(compFlagToBitPositionMapping[typeof(Behaviour)]);
			}
			if (compFlagToBitPositionMapping.TryGetValue(tag.GetType(), out var index) is false) compFlagToBitPositionMapping.Add(tag.GetType(), index = compFlagToBitPositionMapping.Count);
			bitmask.SetFlag(index);
			return ref bitmask;
		}
		public static ref readonly Bitask ClearTag(this in Bitask mask, Component tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);
			if (tag is Renderer)
			{
				bitmask.ClearFlag(compFlagToBitPositionMapping[typeof(Renderer)]);
			}
			else if (tag is CommandBufferComponent)
			{
				bitmask.ClearFlag(compFlagToBitPositionMapping[typeof(CommandBufferComponent)]);
			}
			else if (tag is Behaviour)
			{
				bitmask.ClearFlag(compFlagToBitPositionMapping[typeof(Behaviour)]);
			}
			if (compFlagToBitPositionMapping.TryGetValue(tag.GetType(), out var index))
				bitmask.ClearFlag(index);
			else throw new ArgumentException($"{tag} does not exist.");

			return ref bitmask;
		}
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
		public static void DecomposeDirections(in this Matrix4x4 mat, out Vector3 right, out Vector3 up, out Vector3 forward)
		{
			right = new Vector3(mat.M11, mat.M12, mat.M13);
			up = new Vector3(mat.M21, mat.M22, mat.M23);
			forward = new Vector3(mat.M31, mat.M32, mat.M33);
		}
		public static void OrthoNormalize(in this Matrix4x4 mat)
		{
			ref var matref = ref Unsafe.AsRef(mat);
			var right = new Vector3(mat.M11, mat.M12, mat.M13).Normalize();
			var up = new Vector3(mat.M21, mat.M22, mat.M23).Normalize();
			var forward = new Vector3(mat.M31, mat.M32, mat.M33).Normalize();
			matref.M11 = right.X;
			matref.M12 = right.Y;
			matref.M13 = right.Z;
			matref.M21 = up.X;
			matref.M22 = up.Y;
			matref.M23 = up.Z;
			matref.M31 = forward.X;
			matref.M32 = forward.Y;
			matref.M33 = forward.Z;
		}
		public static T GetInstanceObject<T>(in this Guid id) where T : class
		{
			Root.idToObjectMapping.TryGetValue(id, out var obj);
			return (T)obj;
		}
		/*public static void Dispose(in this Guid id)
		{
			objectToIdMapping.R;
		}*/
		public static void Dispose<T>(this T obj) where T : class
		{
			objectToIdMapping.Remove(obj);
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
		public static IntPtr GetFieldOffset(this FieldInfo fi) => fi.DeclaringType.IsValueType ? GetStructFieldOffset(fi.FieldHandle) : GetFieldOffset(fi.FieldHandle);

		public static IntPtr GetFieldOffset(RuntimeFieldHandle h) =>
									  (IntPtr)(Marshal.ReadInt32(h.Value + (4 + IntPtr.Size)) & 0xFFFFFF);
		public static IntPtr GetStructFieldOffset(RuntimeFieldHandle h) =>
									  (IntPtr)(Marshal.ReadInt32(h.Value + IntPtr.Size) & 0xFFFFFF);
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
		}

		internal void RemoveEngineObject(IEngineObject obj)
		{
			if (obj is Entity e && e.parent is null)
				root.Remove(e);
			removedEntities.Add(obj);
			var id = obj.GetInstanceID();
			idToObjectMapping.Remove(id);
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
		}
	}
}