using System;
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
		private static int BitSize = (sizeof(uint) * 8) - 1;
		private static int ByteSize = 5;  // log_2(BitSize + 1)

		[JsonInclude]
		//[JsonProperty(IsReference = false)]
		private Buffer<uint> bits;
		
		//TODO: remove this after UnscopedRefAttribute introduction and turn bitmask into unmanaged struct with SIMD acceleration
		public bool IsDefault
		{
			get
			{
				foreach (var b in bits)
				{
					if (BitOperations.PopCount(b) > 0)
						return false;
				}
				return true;
			}
		}
		public BitMask(int startValue)
		{
			bits = new Buffer<uint>();
			if (startValue is 1)
				SetAll();
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
			MemoryMarshal.CreateSpan(ref bits[0], Length).Fill(uint.MaxValue);
		}

		/// <summary>
		/// Clears all bits.
		/// </summary>
		public void ClearAll()
		{
			MemoryMarshal.CreateSpan(ref bits[0], Length).Clear();
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
			for (int i = 0; i < Length; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) != 0)
					return false;
			}
			return true;
		}
		public readonly bool HasAllFlags(in BitMask flags)
		{
			for (int i = 0; i < Length; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) != bit)
					return false;
			}
			return true;
		}
		public readonly bool HasAnyFlags(in BitMask flags)
		{
			for (int i = 0; i < Length; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) != 0)
					return true;
			}
			return false;
		}
	}

	//use InlineArray to make customizing bitmask easier
	[InlineArray(BitMask.Length)]
	struct Buffer<T> where T : unmanaged
	{
		private T _element0;
	}
}
