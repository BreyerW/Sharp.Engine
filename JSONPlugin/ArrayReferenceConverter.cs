using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace JSONPlugin
{
	public class ArrayReferenceConverter : JsonConverter<Array>
	{
		public override Array ReadJson(JsonReader reader, Type objectType, Array existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GetGenericArguments()[0];
			var refId =/* reader.Path is refProperty or idProperty ?*/ reader.ReadAsString() /*: null*/;

			while (reader.TokenType is not JsonToken.StartArray) reader.Read();
			reader.Read();
			IList tmp = new List<object>();
			Array reference = null;
			if (refId is not null)
			{
				while (true)
				{
					tmp.Add(serializer.Deserialize(reader, elementType));
					reader.Read();
					if (reader.TokenType is JsonToken.EndArray) { reader.Read(); break; }
					while (!elementType.IsPrimitive && reader.TokenType is not JsonToken.StartObject) reader.Read();
				}
				reference = existingValue ?? serializer.ReferenceResolver.ResolveReference(serializer, refId) as Array;
				if (reference is not null)
				{
					ref var objArr = ref Unsafe.As<Array, object[]>(ref reference);
					Array.Resize(ref objArr, tmp.Count);
					foreach (var i in ..reference.Length)
						reference.SetValue(tmp[i], i);
					return reference;
				}
				else
					existingValue = Activator.CreateInstance(objectType, tmp.Count) as Array;
				serializer.ReferenceResolver.AddReference(serializer, refId, existingValue);
			}
			foreach (var i in ..tmp.Count)
				existingValue.SetValue(tmp[i], i);

			return existingValue;
		}
		public override void WriteJson(JsonWriter writer, Array value, JsonSerializer serializer)
		{

			writer.WriteStartObject();
			if (serializer.ContractResolver.ResolveContract(value.GetType()).IsReference.HasValue is false || serializer.ContractResolver.ResolveContract(value.GetType()).IsReference is true)
				if (!serializer.ReferenceResolver.IsReferenced(serializer, value))
				{
					writer.WritePropertyName(ListReferenceConverter.idProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
					writer.WritePropertyName(ListReferenceConverter.valuesProperty);
					writer.WriteStartArray();
					foreach (var item in value)
					{
						serializer.Serialize(writer, item);
					}
					writer.WriteEndArray();
				}
				else
				{
					writer.WritePropertyName(ListReferenceConverter.refProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				}
			else
			{
				writer.WritePropertyName(ListReferenceConverter.idProperty);
				writer.WriteNull();
				writer.WritePropertyName(ListReferenceConverter.valuesProperty);
				writer.WriteStartArray();
				foreach (var item in value)
				{
					serializer.Serialize(writer, item);
				}
				writer.WriteEndArray();
			}
			writer.WriteEndObject();
		}
	}
}
