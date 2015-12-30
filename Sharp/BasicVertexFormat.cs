using System.Runtime.InteropServices;
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;

namespace Sharp
{
	[Serializable, StructLayout(LayoutKind.Sequential,Pack=0)]
	public struct BasicVertexFormat:IVertex
	{
		private readonly static int stride=Marshal.SizeOf(default(BasicVertexFormat));
		public int Stride{
			get{ 
				return stride;
			}
		}
		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float X;

		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float Y;

		[RegisterAs(VertexAttribute.POSITION,VertexAttribPointerType.Float)]
		public float Z;

		[RegisterAs(VertexAttribute.UV,VertexAttribPointerType.Float)]
		public Vector2 texcoords;
		//public Color<float> Color;
	}
}

/*public IntPtr ConvertToBytes()
		{
			/*unsafe{
				var target = new byte[stride];
				fixed(BasicVertexFormat* p=&this) {
					return (IntPtr)p;
					/*for (int i = 0; i < stride; i++) {
						target [i] = *((byte*)p + i);
					}*
				}*
//return target;   
}*/