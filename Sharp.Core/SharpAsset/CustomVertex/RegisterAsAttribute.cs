using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using Sharp;
using System.Runtime.CompilerServices;

namespace SharpAsset
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RegisterAsAttribute : Attribute
	{
		public static Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>> registeredVertexFormats = new Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>>();

		public int offset;
		public int dimension;
		public int stride;
		public int shaderLocation;
		public VertexAttribute format;
		public AttributeType type;
		//public List<Action<IVertex, object>> generatedFillers = new List<Action<IVertex, object>>();

		public RegisterAsAttribute(VertexAttribute Format, AttributeType Type)
		{
			format = Format;

			switch (format)
			{
				case VertexAttribute.POSITION:
					shaderLocation = 0;
					dimension = 3;
					break;

				case VertexAttribute.COLOR4:
					shaderLocation = 1;
					dimension = 4;
					break;

				case VertexAttribute.UV:
					shaderLocation = 2;
					dimension = 2;
					break;

				case VertexAttribute.NORMAL:
					shaderLocation = 3;
					dimension = 3;
					break;
					//case default: throw new InvalidOperationException(nameof(format) + " have wrong value"); break;
			}
			type = Type;
		}

		public static void ParseVertexFormat(Type type)
		{
			var fields = type.GetFields().Where(
				p => p.GetCustomAttribute<RegisterAsAttribute>() != null);
			int? lastFormat = null;
			var vertFormat = new Dictionary<VertexAttribute, RegisterAsAttribute>();

			foreach (var field in fields)
			{
				var attrib = field.GetCustomAttribute<RegisterAsAttribute>();
				if (lastFormat != (int)attrib.format)
				{
					lastFormat = (int)attrib.format;
					attrib.stride = Marshal.SizeOf(field.FieldType);
					attrib.offset = Marshal.OffsetOf(type, field.Name).ToInt32();
					//attrib.generatedFillers = new List<Action<IVertex, object>>() { DelegateGenerator.GenerateSetter<IVertex>(field) };
					vertFormat.Add(attrib.format, attrib);
				}
				else if (attrib.format == VertexAttribute.POSITION)
				{
					//	dim++; //error prone
					//vertFormat[attrib.format].generatedFillers.Add(DelegateGenerator.GenerateSetter<IVertex>(field));
				}
				//else if (attrib.format == VertexAttribute.UV)
				//	numOfUV++;
			}
			registeredVertexFormats.Add(type, vertFormat);
		}
	}
}