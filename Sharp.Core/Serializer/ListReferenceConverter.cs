using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Sharp.Serializer
{
	public class ListReferenceConverter : JsonConverter
	{
		internal const string refProperty = "$ref";
		internal const string idProperty = "$id";
		internal const string valuesProperty = "$values";

		public override bool CanConvert(Type objectType)
		{
			return
				 objectType == typeof(IList) && !objectType.IsArray;
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GetGenericArguments()[0];
			var refId =/* reader.Path is refProperty or idProperty ?*/ reader.ReadAsString() /*: null*/;

			while (reader.TokenType is not JsonToken.StartArray)
				reader.Read();
			reader.Read();
			IList tmp = new List<object>();
			IList reference = null;
			var eValue = existingValue as IList;
			if (refId is not null)
			{
				while (true)
				{
					tmp.Add(serializer.Deserialize(reader, elementType));
					reader.Read();
					if (reader.TokenType is JsonToken.EndArray) { reader.Read(); break; }
					while (!elementType.IsPrimitive && reader.TokenType is not JsonToken.StartObject)
						reader.Read();
				}
				reference = eValue ?? serializer.ReferenceResolver.ResolveReference(serializer, refId) as IList;
				if (reference is not null)
				{
					reference.Clear();
					eValue = reference;
				}
				else
					eValue = Activator.CreateInstance(objectType) as IList;
				serializer.ReferenceResolver.AddReference(serializer, refId, eValue);
			}

			if (reference is not null)
				foreach (var i in ..tmp.Count)
					eValue.Add(tmp[i]);
			return eValue;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{

			writer.WriteStartObject();
			if (serializer.ContractResolver.ResolveContract(value.GetType()).IsReference.HasValue is false || serializer.ContractResolver.ResolveContract(value.GetType()).IsReference is true)
				if (!serializer.ReferenceResolver.IsReferenced(serializer, value))
				{
					writer.WritePropertyName(idProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
					writer.WritePropertyName(valuesProperty);
					writer.WriteStartArray();
					foreach (var item in value as IList)
					{
						serializer.Serialize(writer, item);
					}
					writer.WriteEndArray();
				}
				else
				{
					writer.WritePropertyName(refProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				}
			else
			{
				writer.WritePropertyName(idProperty);
				writer.WriteNull();
				writer.WritePropertyName(valuesProperty);
				writer.WriteStartArray();
				foreach (var item in value as IList)
				{
					serializer.Serialize(writer, item);
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
	}
}
