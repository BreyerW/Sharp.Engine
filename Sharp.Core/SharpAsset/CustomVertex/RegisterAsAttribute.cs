using PluginAbstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SharpAsset
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RegisterAsAttribute : Attribute
	{
		public static Dictionary<string, VertexAttribute> specialPropertyNames = new()
		{
			["vertex_position"] = VertexAttribute.POSITION,
			["vertex_texcoord"] = VertexAttribute.UV,
			["vertex_normal"] = VertexAttribute.NORMAL,
			["vertex_color"] = VertexAttribute.COLOR4,
		};
		public static Dictionary<Type, (List<RegisterAsAttribute> attribs, Dictionary<VertexAttribute, RegisterAsAttribute> supportedSpecialAttribs)> registeredVertexFormats = new();
		public string shaderLocation;
		public int offset;
		public int stride;
		public int size;
		public AttributeType type;

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

				if (specialPropertyNames.TryGetValue(attrib.shaderLocation, out var specialProperty))
					supportedSpecialAttribs.Add(specialProperty, attrib);

				attrib.size = attrib.type switch
				{
					AttributeType.Float => attrib.stride / Marshal.SizeOf<float>(),
					_ => 1,
				};
				vertFormat.Add(attrib);

			}
			registeredVertexFormats.Add(type, (vertFormat, supportedSpecialAttribs));
		}
	}
}