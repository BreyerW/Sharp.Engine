using System;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Sharp
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RegisterAsAttribute:Attribute
	{
		public static Dictionary<Type,Dictionary<VertexAttribute,RegisterAsAttribute>> registeredVertexFormats=new Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>>();

		public IntPtr offset;
		public VertexAttribute format;
		public VertexAttribPointerType type;

		public RegisterAsAttribute (VertexAttribute Format, VertexAttribPointerType Type)
		{
			format = Format;
			type = Type;
		}
	}
}

