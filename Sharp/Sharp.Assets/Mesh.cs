using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;

namespace Sharp
{
	[Flags]
	public enum SupportedVertexAttributes
	{
		X=1<<0,
		Y=1<<1,
		Z=1<<2,
		COLOR=1<<3, 
		UV=1<<4, //return float[][] so amount of UV will be flexible
		NORMAL=1<<5
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct Mesh<IndexType>:IAsset where IndexType :struct, IConvertible
	{
		public List<IVertex> Vertices;
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}


		public IndexType[] Indices;

		internal int VBO { get; set;}
		internal int EBO { get; set;}
		internal int TBO { get; set;}

		public BufferUsageHint UsageHint;

		public Matrix4 ModelMatrix;
		public Matrix4 MVPMatrix;

		public BoundingBox bounds;

		public override string ToString ()
		{
			return Name;
		}
	}
}

