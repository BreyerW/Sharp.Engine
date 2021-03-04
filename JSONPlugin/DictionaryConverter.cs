using Newtonsoft.Json;
using System;
using System.Collections;
using System.Linq;

namespace JSONPlugin
{
	public class DictionaryConverter : JsonConverter<IDictionary>
	{
		public override IDictionary ReadJson(JsonReader reader, Type objectType, IDictionary existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var id = reader.ReadAsString();
			if (existingValue is null)
			{
				existingValue = serializer.ContractResolver.ResolveContract(objectType).DefaultCreator() as IDictionary;
				serializer.ReferenceResolver.AddReference(serializer, id, existingValue);
			}
			existingValue.Clear();
			ReadOnlySpan<char> name = "";
			int dotPos = -1;
			if (reader.TokenType == JsonToken.String)
			{
				dotPos = reader.Path.LastIndexOf('.');
				if (dotPos > -1)
					name = reader.Path.AsSpan()[0..dotPos];
			}
			while (reader.Read())
			{
				if (reader.Path.AsSpan().SequenceEqual(name)) break;
				if (reader.TokenType == JsonToken.EndArray /*&& reader.Value as string is "pairs"*/)
				{
					reader.Read();
					break;
				}
				while (reader.Value as string is not "key") reader.Read();
				reader.Read();
				var generics = objectType.GetGenericArguments();
				var key = serializer.Deserialize(reader, generics[0]);
				while (reader.Value as string is not "value") reader.Read();
				reader.Read();
				var value = serializer.Deserialize(reader, generics[1]);
				existingValue.Add(key, value);
				reader.Read();
			}
			return existingValue;
		}

		public override void WriteJson(JsonWriter writer, IDictionary value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ListReferenceConverter.idProperty);
			writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
			writer.WritePropertyName("pairs");
			writer.WriteStartArray();
			foreach (DictionaryEntry item in value)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("key");
				serializer.Serialize(writer, item.Key);
				writer.WritePropertyName("value");
				serializer.Serialize(writer, item.Value);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
}
