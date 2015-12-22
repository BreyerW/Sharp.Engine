using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;

namespace Sharp
{
	[Flags]
	public enum VertexAttribute
	{
		POSITION=1<<0,
		COLOR=1<<1, 
		UV=1<<2, //return float[][] so amount of UV will be flexible
		NORMAL=1<<3,
		CUSTOM=1<<4
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct Mesh<IndexType>:IAsset where IndexType :struct, IConvertible
	{
		public IVertex[] Vertices;
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}


		public IndexType[] Indices;

		internal int VBO { get; set;}
		internal int EBO { get; set;}
		internal int TBO { get; set;}

		public BufferUsageHint UsageHint;

		public BoundingBox bounds;

		public override string ToString ()
		{
			return Name;
		}
	}
}

