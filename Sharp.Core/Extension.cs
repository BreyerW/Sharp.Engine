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
using System.Linq;

namespace Sharp
{
	public static class Extension
	{
		private static Dictionary<string, int> flagToBitPositionMapping = new()//TODO: replace with generated enum?
		{
		};
		private static Dictionary<Type, int> compFlagToBitPositionMapping = new()
		{
		};
		private static Dictionary<Type, BitMask> abstractCompToBitMaskMapping = new()
		{
			[typeof(Renderer)] = new BitMask(0),
			[typeof(Behaviour)] = new BitMask(0),
			[typeof(CommandBufferComponent)] = new BitMask(0)
		};
		[ModuleInitializer]
		internal static void LoadAbstractComponents()
		{
			var baseType = typeof(Component);
			var types = baseType.Assembly.GetTypes().Where(t => t != baseType && !t.IsAbstract &&
											baseType.IsAssignableFrom(t));
			var index = 0;
			foreach (var t in types)
			{
				compFlagToBitPositionMapping.Add(t, index = compFlagToBitPositionMapping.Count);
				//TODO: use source generators to populate dictionary instead
				if (typeof(Renderer).IsAssignableFrom(t))
				{
					abstractCompToBitMaskMapping[typeof(Renderer)].SetFlag(index);
				}
				else if (typeof(CommandBufferComponent).IsAssignableFrom(t))
				{
					abstractCompToBitMaskMapping[typeof(CommandBufferComponent)].SetFlag(index);
				}
				else if (typeof(Behaviour).IsAssignableFrom(t))
				{
					abstractCompToBitMaskMapping[typeof(Behaviour)].SetFlag(index);
				}
			}
		}
		public static BitMask GetBitMaskFor<T>() where T : Component
		{
			return abstractCompToBitMaskMapping[typeof(T)];
		}
		public static ref readonly BitMask SetTag(this in BitMask mask, string tag)
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
		public static ref readonly BitMask ClearTag(this in BitMask mask, string tag)
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
		public static ref readonly BitMask SetTag(this in BitMask mask, Component tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);

			/*if (compFlagToBitPositionMapping.TryGetValue(tag.GetType(), out var index) is false)
			{
				compFlagToBitPositionMapping.Add(tag.GetType(), index = compFlagToBitPositionMapping.Count);
			}*/
			bitmask.SetFlag(compFlagToBitPositionMapping[tag.GetType()]);
			return ref bitmask;
		}
		public static ref readonly BitMask ClearTag(this in BitMask mask, Component tag)
		{
			ref var bitmask = ref Unsafe.AsRef(mask);
			if (compFlagToBitPositionMapping.TryGetValue(tag.GetType(), out var index))
				bitmask.ClearFlag(index);
			else throw new ArgumentException($"{tag} does not exist.");

			return ref bitmask;
		}
		internal static Root entities = new Root();

		public static Guid GetInstanceID<T>(this T obj) where T : class
		{
			if (!PluginManager.serializer.objToIdMapping.TryGetValue(obj, out var id))
			{
				PluginManager.serializer.objToIdMapping.Add(obj, id = Guid.NewGuid());
				//throw new InvalidOperationException("attempted to add new entity this shouldnt be happening");
			}
			return id;
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
			return PluginManager.serializer.objToIdMapping.GetKey(id) as T;
			//Root.idToObjectMapping.TryGetValue(id, out var obj);
			//return (T)obj;
		}
		/*public static void Dispose(in this Guid id)
		{
			objectToIdMapping.R;
		}*/
		public static void Dispose<T>(this T obj) where T : class
		{
			PluginManager.serializer.objToIdMapping.Remove(obj);
		}
		public static IEngineObject GetInstanceObject(in this Guid id)
		{
			return PluginManager.serializer.objToIdMapping.GetKey(id) as IEngineObject;
			//Root.idToObjectMapping.TryGetValue(id, out var obj);
			//return obj;
		}
		internal static void AddRestoredObject<T>(this T obj, in Guid id) where T : class
		{
			PluginManager.serializer.objToIdMapping.Add(obj, id);
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

		internal void AddEngineObject(IEngineObject obj)
		{
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
		}
		internal void AddRestoredEngineObject(IEngineObject obj, in Guid id)
		{
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