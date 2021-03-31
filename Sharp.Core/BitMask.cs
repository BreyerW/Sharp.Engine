using System;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading;

namespace Sharp.Core
{
	/// <summary>
	/// A resizable collection of bits.
	/// </summary>
	public struct BitMask
	{
		private static int BitSize = (sizeof(uint) * 8) - 1;
		private static int ByteSize = 5;  // log_2(BitSize + 1)

		[JsonInclude]
		//[JsonProperty(IsReference = false)]
		private uint[] bits;

		public BitMask(int startValue)
		{
			bits = new uint[1];
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
			if (b >= bits.Length)
				Array.Resize(ref bits, b + 1);

			Interlocked.Or(ref bits[b], 1u << (index & BitSize));
		}
		/// <summary>
		/// Clears the bit at the given index.
		/// </summary>
		/// <param name="index">The bit to clear.</param>
		public void ClearFlag(int index)
		{
			int b = index >> ByteSize;
			if (b >= bits.Length)
				return;

			Interlocked.And(ref bits[b], ~(1u << (index & BitSize)));
		}
		/// <summary>
		/// Sets all bits.
		/// </summary>
		public void SetAll()
		{
			bits = Array.Empty<uint>();
			//int count = bits.Length;
			//for (int i = 0; i < count; i++)
			//bits[i] = 0xffffffff;
		}

		/// <summary>
		/// Clears all bits.
		/// </summary>
		public void ClearAll()
		{
			Array.Clear(bits, 0, bits.Length);
		}

		/// <summary>
		/// Determines whether the given bit is set.
		/// </summary>
		/// <param name="index">The index of the bit to check.</param>
		/// <returns><c>true</c> if the bit is set; otherwise, <c>false</c>.</returns>
		public readonly bool IsSet(int index)
		{
			int b = index >> ByteSize;
			if (b >= bits.Length)
				return false;

			return (bits[b] & (1 << (index & BitSize))) != 0;
		}

		public readonly bool HasNoFlags(in BitMask flags)
		{
			if (flags.bits.Length is 0)//means Everything
				return false;
			//var isNothing = true;
			//foreach (var bit in flags.bits.AsSpan())
			//if (BitOperations.PopCount(bit) is not 0)//or (bit&0) is not 0
			//	isNothing = false;
			//if (isNothing) return true;
			int count = bits.Length;
			int flagsCount = flags.bits.Length;
			if (flagsCount < count)
				count = flagsCount;
			for (int i = 0; i < count; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) is not 0)
					return false;
			}
			return true;
		}
		public readonly bool HasAllFlags(in BitMask flags)
		{
			if (flags.bits.Length is 0)
				return true;
			int count = bits.Length;
			int flagsCount = flags.bits.Length;
			if (flagsCount > count)
			{
				foreach (var bit in flags.bits.AsSpan()[count..])
					if (BitOperations.PopCount(bit) is not 0)//or (bit&0) is not 0
						return false; //early out in case testing mask has flags set in last bits that are outside of range of this mask
			}
			else
				count = flagsCount;
			for (int i = 0; i < count; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) != bit)
					return false;
			}
			return true;
		}
		public readonly bool HasAnyFlags(in BitMask flags)
		{
			if (flags.bits is null)
				throw new ArgumentNullException(nameof(flags.bits));
			int count = bits.Length;
			int flagsCount = flags.bits.Length;
			if (flagsCount < count)
				count = flagsCount;
			for (int i = 0; i < count; i++)
			{
				uint bit = flags.bits[i];
				if ((bits[i] & bit) == 0)
					return false;
			}
			return true;
		}
	}
}
