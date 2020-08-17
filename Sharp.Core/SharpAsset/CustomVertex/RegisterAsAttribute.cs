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
		public static Dictionary<string, VertexAttribute> specialPropertyNames = new Dictionary<string, VertexAttribute>()
		{
			["vertex_position"] = VertexAttribute.POSITION,
			["vertex_texcoord"] = VertexAttribute.UV,
			["vertex_normal"] = VertexAttribute.NORMAL,
			["vertex_color"] = VertexAttribute.COLOR4,
		};
		public static Dictionary<Type, (List<RegisterAsAttribute> attribs, Dictionary<VertexAttribute, RegisterAsAttribute> supportedSpecialAttribs)> registeredVertexFormats = new Dictionary<Type, (List<RegisterAsAttribute> attribs, Dictionary<VertexAttribute, RegisterAsAttribute> supportedSpecialAttribs)>();
		public string shaderLocation;
		public int offset;
		public int stride;
		public int size;
		public AttributeType type;
		//public List<Action<IVertex, object>> generatedFillers = new List<Action<IVertex, object>>();

		public RegisterAsAttribute(AttributeType Type, string customAttributeLocation = "")
		{
			shaderLocation = customAttributeLocation;
			type = Type;
		}
		public static void ParseVertexFormat(Type type)
		{
			var fields = type.GetFields().Where(
				p => p.GetCustomAttribute<RegisterAsAttribute>() != null);
			var vertFormat = new List<RegisterAsAttribute>();
			var supportedSpecialAttribs = new Dictionary<VertexAttribute, RegisterAsAttribute>();
			foreach (var field in fields)
			{
				var attrib = field.GetCustomAttribute<RegisterAsAttribute>();
				attrib.shaderLocation = attrib.shaderLocation is "" ? field.Name : attrib.shaderLocation;
				attrib.stride = Marshal.SizeOf(field.FieldType);
				attrib.offset = Marshal.OffsetOf(type, field.Name).ToInt32();
				var isSepecial = specialPropertyNames.TryGetValue(attrib.shaderLocation, out var specialProperty);
				if (isSepecial)
				{
					attrib.size = specialProperty switch
					{
						VertexAttribute.POSITION => 3,
						VertexAttribute.COLOR4 => 4,
						VertexAttribute.UV => 2,
						VertexAttribute.NORMAL => 3,
						_ => 1,
					};
					supportedSpecialAttribs.Add(specialProperty, attrib);
				}
				else
					attrib.size = 1;
				//attrib.generatedFillers = new List<Action<IVertex, object>>() { DelegateGenerator.GenerateSetter<IVertex>(field) };
				vertFormat.Add(attrib);

			}
			registeredVertexFormats.Add(type, (vertFormat, supportedSpecialAttribs));
		}
	}
}