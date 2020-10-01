using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Core
{
	/// <summary>
	/// A resizable collection of bits.
	/// </summary>
	public class BitMask
	{
		const int BitSize = (sizeof(uint) * 8) - 1;
		const int ByteSize = 5;  // log_2(BitSize + 1)

		uint[] bits;

		/// <summary>
		/// Initializes a new instance of the <see cref="BitSet"/> class.
		/// </summary>
		public BitMask()
		{
			bits = new uint[1];
		}

		/// <summary>
		/// Determines whether the given bit is set.
		/// </summary>
		/// <param name="index">The index of the bit to check.</param>
		/// <returns><c>true</c> if the bit is set; otherwise, <c>false</c>.</returns>
		public bool IsSet(int index)
		{
			int b = index >> ByteSize;
			if (b >= bits.Length)
				return false;

			return (bits[b] & (1 << (index & BitSize))) != 0;
		}

		/// <summary>
		/// Sets the bit at the given index.
		/// </summary>
		/// <param name="index">The bit to set.</param>
		public void SetBit(int index)
		{
			int b = index >> ByteSize;
			if (b >= bits.Length)
				Array.Resize(ref bits, b + 1);

			bits[b] |= 1u << (index & BitSize);
		}

		/// <summary>
		/// Clears the bit at the given index.
		/// </summary>
		/// <param name="index">The bit to clear.</param>
		public void ClearBit(int index)
		{
			int b = index >> ByteSize;
			if (b >= bits.Length)
				return;

			bits[b] &= ~(1u << (index & BitSize));
		}

		/// <summary>
		/// Sets all bits.
		/// </summary>
		public void SetAll()
		{
			int count = bits.Length;
			for (int i = 0; i < count; i++)
				bits[i] = 0xffffffff;
		}

		/// <summary>
		/// Clears all bits.
		/// </summary>
		public void ClearAll()
		{
			Array.Clear(bits, 0, bits.Length);
		}

		/// <summary>
		/// Determines whether all of the bits in this instance are also set in the given bitset.
		/// </summary>
		/// <param name="other">The bitset to check.</param>
		/// <returns><c>true</c> if all of the bits in this instance are set in <paramref name="other"/>; otherwise, <c>false</c>.</returns>
		public bool IsSubsetOf(BitMask other)
		{
			if (other == null)
				throw new ArgumentNullException("other");

			var otherBits = other.bits;
			int count = Math.Min(bits.Length, otherBits.Length);
			for (int i = 0; i < count; i++)
			{
				uint bit = bits[i];
				if ((bit & otherBits[i]) != bit)
					return false;
			}

			// handle extra bits on our side that might just be all zero
			int extra = bits.Length - count;
			for (int i = count; i < extra; i++)
			{
				if (bits[i] != 0)
					return false;
			}

			return true;
		}
		public bool HasFlags(BitMask flags)
		{
			if (flags == null)
				throw new ArgumentNullException("other");

			var otherBits = flags.bits;
			int count = Math.Min(bits.Length, otherBits.Length);
			for (int i = 0; i < count; i++)
			{
				uint bit = otherBits[i];
				if ((bits[i] & bit) != bit)
					return false;
			}

			return true;
		}
	}
}
