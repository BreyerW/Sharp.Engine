using PluginAbstraction;
using Sharp;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset
{
	[Serializable]
	public struct Texture : IAsset //TODO: split for texture - class, bitmap - struct? similar to mesh - class vertexformats - structs
	{
		//internal bool allocated;
		//internal int TBO;
		internal IntPtr DataAddr
		{
			get
			{
				unsafe
				{
					return (IntPtr)Unsafe.AsPointer(ref data[0]);
				}
			}
		}
		internal ref int TBO { get { return ref Unsafe.As<byte, int>(ref data[0]); } }
		public int width;
		public int height;
		public bool normalized;
		public TextureFormat format;
		internal byte[] data;

		public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }
		public string FullPath { get; set; }

		public override string ToString()
		{
			return Name.ToString();
		}
	}
}