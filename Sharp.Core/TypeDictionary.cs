using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp.Core
{
	public struct TypeDictionary<TValue>
	{
		private static class TypeSlot<T>
		{
			internal static readonly int Index = Interlocked.Increment(ref TypeIndex);
		}

		private static volatile int TypeIndex = -1;

		private TValue[] storage;

		public TypeDictionary()
		{
			storage = new TValue[Math.Max(1, TypeIndex + 1)];
		}

		private TValue[] EnsureStorageCapacity<T>()
		{
			if (TypeSlot<T>.Index >= storage.Length)
				Array.Resize(ref storage, TypeSlot<T>.Index + 1);

			return storage;
		}

		public void Set<T>(TValue value)
		{
			Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(EnsureStorageCapacity<T>()), TypeSlot<T>.Index) = value;
		}

		public ref TValue Get<T>()
		{
			return ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(EnsureStorageCapacity<T>()), TypeSlot<T>.Index);
		}
	}
}
