using System.Runtime.InteropServices;
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;
using SharpAsset;

namespace Sharp
{
	[Serializable, StructLayout(LayoutKind.Sequential,Pack=0)]
	public struct BasicVertexFormat:IVertex
	{
		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float X;

		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float Y;

		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float Z;

		[RegisterAs(VertexAttribute.UV,VertexAttribPointerType.Float)]
		public Vector2 texcoords;
		//public Color<float> Color;

	/*	public byte[] ConvertToBytes()
		{
			unsafe{
				var target = new byte[Stride];
				fixed(BasicVertexFormat* p=&this) {
					var bytePtr = (byte*)p;
					for (int i = 0; i < Stride; i++) {
						target [i] = *(bytePtr + i);
					}
				}
return target;   
	}
	}*/
	}
}
