using Sharp;
using System;
using System.IO;
using System.Numerics;

namespace SharpAsset
{
	public enum TextureFormat
	{
		R,
		RGB,
		RGBA,
		RUInt,
		RGUInt,
		RGBUInt,
		RGBAUInt,
		A,
		RGBAFloat,
		RG16_SNorm,
		DepthFloat,
	}
	[Serializable]
	public struct Texture : IAsset //TODO: split for texture - class, bitmap - struct? similar to mesh - class vertexformats - structs
	{
		//internal bool allocated;
		internal int TBO;
		internal int FBO;
		public int width;
		public int height;
		public byte bits;
		public bool normalized;
		public TextureFormat format;
		internal byte[] bitmap;

		public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }
		public string FullPath { get; set; }

		public override string ToString()
		{
			return Name.ToString();
		}

		public void PlaceIntoScene(Entity context, Vector3 worldPos)
		{
			throw new NotImplementedException();
		}
	}
}