using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics.Tensors;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp.Core
{
	//TODO: add bitwise or and and similar operators with ref 
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
				return TensorPrimitives.IsZeroAll<uint>(bits.AsSpan());
				/*for (int i = 0; i < Length; i++)
				{
					if (bits[i] != default)
						return false;
				}
				return true;*/
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
		public void SetFlag(in BitMask mask)
		{
			TensorPrimitives.BitwiseOr(mask.bits.AsSpan(), bits.AsSpan(), bits.AsSpan());
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
		[SkipLocalsInit]
		public void ClearFlag(in BitMask mask)
		{
			Span<uint> results = stackalloc uint[Length];
			TensorPrimitives.OnesComplement(mask.bits.AsSpan(), results);
			TensorPrimitives.BitwiseAnd(results, bits.AsSpan(), bits.AsSpan());
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
		[SkipLocalsInit]
		public readonly bool HasNoFlags(in BitMask flags)
		{
			Span<uint> results = stackalloc uint[Length];
			TensorPrimitives.BitwiseAnd(flags.bits.AsSpan(), bits.AsSpan(), results);
			return TensorPrimitives.IsZeroAll<uint>(results);
			/*for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.BitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != Vector512<uint>.Zero)
					return false;
			}
			return true;*/
		}
		[SkipLocalsInit]
		public readonly bool HasAllFlags(in BitMask flags)
		{
			Span<uint> results = stackalloc uint[Length];
			//since we use uint to store bits, we can use subtraction to check if all flags are set. If all flags are set, the result will be zero.
			TensorPrimitives.Subtract(flags.bits.AsSpan(), bits.AsSpan(), results);
			return TensorPrimitives.IsZeroAll<uint>(results);
			/*TensorPrimitives.BitwiseAnd(flags.bits.AsSpan(), bits.AsSpan(), results);
			for (int i = 0; i < results.Length; i++)
			{
				if (results[i] != flags.bits[i])
					return false;
			}*/
			/*for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.equaBitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != bit)
					return false;
			}*/
			//return true;
		}
		[SkipLocalsInit]
		public readonly bool HasAnyFlags(in BitMask flags)
		{
			Span<uint> results = stackalloc uint[Length];
			TensorPrimitives.BitwiseAnd(flags.bits.AsSpan(), bits.AsSpan(), results);
			return !TensorPrimitives.IsZeroAll<uint>(results);
			/*for (int i = 0; i < VectorLength; i++)
			{
				var vectorId = i * Length;
				var bit = Vector512.LoadUnsafe(ref flags.bits.GetElement(vectorId));
				if (Vector512.BitwiseAnd(Vector512.LoadUnsafe(ref bits.GetElement(vectorId)), bit) != Vector512<uint>.Zero)
					return true;
			}
			return false;*/
		}
	}

	[InlineArray(BitMask.Length)]
	struct Buffer<T> where T : unmanaged
	{
		//[UnscopedRef]
		//public ref T GetElement(int index) => ref Unsafe.Add(ref Unsafe.As<Buffer<T>, T>(ref this), index);
		public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref _element0, BitMask.Length);

		private T _element0;
	}
}
