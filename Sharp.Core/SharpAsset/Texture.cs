using Sharp;
using System;
using System.IO;
using System.Numerics;

namespace SharpAsset
{
	public enum TextureFormat
	{
		RGB,
		RGBA,
		A
	}
	[Serializable]
	public struct Texture : IAsset //TODO: split for texture - class, bitmap - struct? similar to mesh - class vertexformats - structs
	{
		//internal bool allocated;
		internal int TBO;
		internal int FBO;
		public int width;
		public int height;
		public TextureFormat format;
		internal byte[] bitmap;

		public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public string Extension { get { return Path.GetExtension(FullPath); } set { } }
		public string FullPath { get; set; }

		public override string ToString()
		{
			return Name;
		}

		public void PlaceIntoScene(Entity context, Vector3 worldPos)
		{
			throw new NotImplementedException();
		}
	}
}