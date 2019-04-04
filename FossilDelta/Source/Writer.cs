using System.Collections.Generic;
using System.IO;
using System.Buffers;
using System;

namespace Fossil
{
  public class Writer : IDisposable
  {
	private static readonly uint[] zDigits = {
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D',
			'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R',
			'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '_', 'a', 'b', 'c', 'd', 'e',
			'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's',
			't', 'u', 'v', 'w', 'x', 'y', 'z', '~'
		};

	private byte[] a;
	private uint pos = 0;
	private uint[] zBuf = new uint[20];

	public Writer(int expectedMaxSize)
	{
	  this.a = ArrayPool<byte>.Shared.Rent(expectedMaxSize); //TODO: add arraypool<byte> instead of list<byte>
	}

	public void PutChar(char c)
	{
	  this.a[pos] = (byte)c;
	  pos++;
	}

	public void PutInt(uint v)
	{
	  int i, j;

	  if (v == 0)
	  {
		this.PutChar('0');
		return;
	  }
	  for (i = 0; v > 0; i++, v >>= 6)
	  {
		zBuf[i] = zDigits[v & 0x3f];
	  }
	  for (j = i - 1; j >= 0; j--)
	  {
		this.a[pos] = (byte)zBuf[j];
		pos++;
	  }
	}

	public void PutArray(byte[] a, int start, int end)
	{
	  for (var i = start; i < end; i++)
	  {
		this.a[pos] = a[i];
		pos++;
	  }
	}

	public void PutArray(Stream a, int start, int end)
	{
	  a.Position = start;
	  for (var i = start; i < end; i++)
	  {
		this.a[pos] = (byte)a.ReadByte();
		pos++;
	  }
	}

	public byte[] ToArray(out uint bufferLength)
	{
	  bufferLength = pos;
	  return a;
	}

	#region IDisposable Support

	private bool disposedValue = false; // To detect redundant calls

	protected virtual void Dispose(bool disposing)
	{
	  if (!disposedValue)
	  {
		if (disposing)
		{
		  ArrayPool<byte>.Shared.Return(a);
		}
		//a = null;
		disposedValue = true;
	  }
	}

	// This code added to correctly implement the disposable pattern.
	public void Dispose()
	{
	  // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
	  Dispose(true);
	}

	#endregion IDisposable Support
  }
}