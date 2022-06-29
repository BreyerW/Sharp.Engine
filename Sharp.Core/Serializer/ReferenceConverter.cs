
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Serializer
{
	public class ReferenceConverter : JsonConverter
	{
		public override bool CanWrite => false;
		public override bool CanConvert(Type typeToConvert)
		{
			return (typeToConvert.IsClass || typeToConvert.IsInterface) && typeToConvert != typeof(string);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			reader.Read();
			if (reader.TokenType is JsonToken.StartObject)
				reader.Read();
			var id = string.Empty;
			var valueId = string.Empty;
			if (reader.Value is string and "$id" or "$ref")
			{
				id = reader.Value as string;
				reader.Read();
				valueId = reader.Value as string;
				reader.Read();
			}
			if (reader.Value is string and "$type")
			{
				reader.Read();
				reader.Read();
			}
			var contract = serializer.ContractResolver.ResolveContract(objectType) as JsonObjectContract;
			var target = serializer.ReferenceResolver.ResolveReference(null, valueId) ?? RuntimeHelpers.GetUninitializedObject(objectType);
			if (id is "$ref")
			{
				reader.Read();
				return target;
			}
			else
				serializer.ReferenceResolver.AddReference(null, valueId, target);
			var depth = reader.Depth;
			while (reader.TokenType is not JsonToken.EndObject)
			{
				var propName = (string)reader.Value;
				reader.Read();
				var p = contract.Properties.GetClosestMatchProperty(propName);
				if (p.Ignored)
				{
					reader.Skip();
					reader.Read();
				}
				else
				{
					p.ValueProvider.SetValue(target, serializer.Deserialize(reader, p.PropertyType));
					//while (depth != reader.Depth && reader.TokenType is not JsonToken.PropertyName)
					if (reader.TokenType is not JsonToken.PropertyName)
						reader.Read();
				}
			}
			return target;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
