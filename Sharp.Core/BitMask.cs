using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using System.Threading;

namespace Sharp.Core
{
	//TODO: use vectorized Vector128/256 when supported and when bits longer than vectors? 
	/// <summary>
	/// A resizable collection of bits.
	/// </summary>
	public struct BitMask
	{
		public const int Length = 16;// * sizeof(uint);
		
		// Vector512<uint>.Count == 16
		private const int VectorLength = Length / 16; 
		private static int BitSize = (sizeof(uint) * 8) - 1;
		private static int ByteSize = 5;  // log_2(BitSize + 1)

		[JsonInclude]
		//[JsonProperty(IsReference = false)]
		private Buffer<uint> bits;

		public bool IsDefault
		{
			get
			{
				for (int i = 0; i < Length; i++)
				{
					if (bits[i] != default)
						return false;
				}
				return true;
			}
		}
		public BitMask(int startValue)
		{
			if (startValue is 1)
				SetAll();
			else
				ClearAll();
		}
		/// <summary>
		/// Sets the bit at the given index.
		/// </summary>
		/// <param name="index">The bit to set.</param>
		public void SetFlag(int index)
		{
			int b = index >> ByteSize;
			if (b >= Length)
				throw new ArgumentException("Bitmask is too small to handle this operation. Increase bitmask size to fix this.");

			Interlocked.Or(ref bits[b], 1u << (index & BitSize));
		}
		/// <summary>
		/// Clears the bit at the given index.
		/// </summary>
		/// <param name="index">The bit to clear.</param>
		public void ClearFlag(int index)
		{
			int b = index >> ByteSize;
			if (b >= Length)
				throw new ArgumentException("Bitmask is too small to handle this operation. Increase bitmask size to fix this.");

			Interlocked.And(ref bits[b], ~(1u << (index & BitSize)));
		}
		/// <summary>
		/// Sets all bits.
		/// </summary>
		public void SetAll()
		{
			//Vector512<uint>.AllBitsSet.StoreUnsafe(ref bits[0]);
			MemoryMarshal.CreateSpan(ref bits[0], Length).Fill(uint.MaxValue);
		}

		/// <summary>
		/// Clears all bits.
		/// </summary>
		public void ClearAll()
		{
			bits = default;
		}

		/// <summary>
		/// Determines whether the given bit is set.
		/// </summary>
		/// <param name="index">The index of the bit to check.</param>
		/// <returns><c>true</c> if the bit is set; otherwise, <c>false</c>.</returns>
		public readonly bool IsSet(int index)
		{
			int b = index >> ByteSize;
			if (b >= Length)
				throw new ArgumentException("Bitmask is too small to handle this operation. Increase bitmask size to fix this.");
			return (bits[b] & (1 << (index & BitSize))) != 0;
		}

		public readonly bool HasNoFlags(in BitMask flags)
		{
			for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.BitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != Vector512<uint>.Zero)
					return false;
			}
			return true;
		}
		public readonly bool HasAllFlags(in BitMask flags)
		{
			for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.BitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != bit)
					return false;
			}
			return true;
		}
		public readonly bool HasAnyFlags(in BitMask flags)
		{
			for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.BitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != Vector512<uint>.Zero)
					return true;
			}
			return false;
		}
	}

	//use InlineArray to make customizing bitmask easier
	[InlineArray(BitMask.Length)]
	struct Buffer<T> where T : unmanaged
	{
		[UnscopedRef]
		public ref T GetElement(int index) => ref Unsafe.Add(ref Unsafe.As<Buffer<T>, T>(ref this), index);

		private T _element0;
	}
}
