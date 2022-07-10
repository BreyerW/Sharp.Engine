
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Serializer
{
	//TODO: support for ICollection types
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
				objectType = Type.GetType(reader.Value as string);
				reader.Read();
			}
			var target = serializer.ReferenceResolver.ResolveReference(null, valueId) ?? RuntimeHelpers.GetUninitializedObject(objectType);
			if (id is "$ref")
			{
				return target;
			}
			else
				serializer.ReferenceResolver.AddReference(null, valueId, target);

			var c = serializer.ContractResolver.ResolveContract(objectType);
			if (c is JsonObjectContract contract)
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
						if (reader.TokenType is not JsonToken.PropertyName)
							reader.Read();
					}
				}
			else if (c is JsonArrayContract arrayContract)
			{
				if (target is IList list and { IsReadOnly: false })
				{
					reader.Read();
					reader.Read();
					list.Clear();
					if (reader.TokenType is not JsonToken.EndArray)
						while (true)
						{
							list.Add(serializer.Deserialize(reader, arrayContract.CollectionItemType));
							reader.Read();
							if (reader.TokenType is JsonToken.EndArray) { reader.Read(); break; }
							//while (!arrayContract.CollectionItemType.IsPrimitive && reader.TokenType is not JsonToken.StartObject) reader.Read();
						}
					else
						reader.Read();
				}
				else
				{
					reader.Skip();
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
