using System.Runtime.InteropServices;
using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Sharp
{
	[StructLayout(LayoutKind.Sequential)]
	public struct BasicVertexFormat:IVertex
	{
		//public static VertexFormat format=VertexFormat.X|VertexFormat.Y|VertexFormat.Z|VertexFormat.COLOR;

		public float X;
		public float Y;
		public float Z;

		//public Color<float> Color;

		public Vector2 texcoords;

		public void RegisterAttributes<IndexType>() where IndexType :struct, IConvertible {
			MeshRenderer<IndexType,BasicVertexFormat>.RegisterAttribute (SupportedVertexAttributes.X|SupportedVertexAttributes.Y|SupportedVertexAttributes.Z,VertexAttribPointerType.Float);
			//MeshRenderer<IndexType,BasicVertexFormat>.RegisterAttribute (SupportedVertexAttributes.COLOR,VertexAttribPointerType.Float);
			MeshRenderer<IndexType,BasicVertexFormat>.RegisterAttribute (SupportedVertexAttributes.UV,VertexAttribPointerType.Float);
		}
	}
}

