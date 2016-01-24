using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq.Expressions;

namespace SharpAsset
{
	[AttributeUsage(AttributeTargets.Field)]
	public class RegisterAsAttribute:Attribute
	{
		public static Dictionary<Type,Dictionary<VertexAttribute,RegisterAsAttribute>> registeredVertexFormats=new Dictionary<Type, Dictionary<VertexAttribute, RegisterAsAttribute>>();

		public IntPtr offset;
		public int Dimension{
			get{ 
				switch (format) {
				case VertexAttribute.POSITION:
					return 3;//generatedFillers.Count;
				case VertexAttribute.COLOR:
					return 4;
				case VertexAttribute.UV:
					return 2;
				case VertexAttribute.NORMAL:
					return 3;
				}
				throw new InvalidOperationException (nameof(format)+" have wrong value");
			}
		}
		public int shaderLocation;
		public VertexAttribute format;
		public VertexType type;
		public List<Action<IVertex,object>> generatedFillers=new List<Action<IVertex,object>>(); 

		public RegisterAsAttribute (VertexAttribute Format, VertexType Type)
		{
			format = Format;

			switch (format) {
			case VertexAttribute.POSITION:
				shaderLocation=0;
				break;
			case VertexAttribute.COLOR:
				shaderLocation=1;
				break;
			case VertexAttribute.UV:
				shaderLocation=2;
				break;
			case VertexAttribute.NORMAL:
				shaderLocation=3;
				break;
			}
			type = Type;
		}
		public static void ParseVertexFormat(Type type){
				var fields=type.GetFields ().Where (
					p => p.GetCustomAttribute<RegisterAsAttribute>()!=null);
				int? lastFormat=null;
				var vertFormat = new Dictionary<VertexAttribute,RegisterAsAttribute> ();

				foreach(var field in fields){
					var attrib=field.GetCustomAttribute<RegisterAsAttribute>();
					if (lastFormat != (int)attrib.format) {
						lastFormat = (int)attrib.format;
						attrib.offset = Marshal.OffsetOf(type,field.Name);
						attrib.generatedFillers=new List<Action<IVertex, object>>(){GenerateOpenSetter(field)};
						vertFormat.Add (attrib.format,attrib);

					} else if (attrib.format == VertexAttribute.POSITION){
					//	dim++; //error prone
					vertFormat[attrib.format].generatedFillers.Add(GenerateOpenSetter(field));
					}
					//else if (attrib.format == VertexAttribute.UV)
					//	numOfUV++;
				}
				RegisterAsAttribute.registeredVertexFormats.Add (type,vertFormat);

		}
		private static Action<IVertex, object> GenerateOpenSetter(FieldInfo fieldInfo)
		{            
			//parameter "target", the object on which to set the field `field`
			ParameterExpression targetExp = Expression.Parameter(typeof(IVertex), "target");

			//parameter "value" the value to be set in the `field` on "target"
			ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");

			//cast the target from object to its correct type
			Expression castTartgetExp = Expression.Unbox(targetExp,fieldInfo.DeclaringType);

			//cast the value to its correct type
			Expression castValueExp = Expression.Unbox(valueExp, fieldInfo.FieldType);

			//the field `field` on "target"
			MemberExpression fieldExp = Expression.Field(castTartgetExp , fieldInfo);

			//assign the "value" to the `field` 
			BinaryExpression assignExp = Expression.Assign(fieldExp, castValueExp);

			//compile the whole thing
			return Expression.Lambda<Action<IVertex, object>> (assignExp, targetExp, valueExp).Compile();
		}
	}
}

