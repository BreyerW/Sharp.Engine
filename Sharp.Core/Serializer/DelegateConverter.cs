using Newtonsoft.Json;
using System;

namespace Sharp.Serializer
{
    public class DelegateConverter : JsonConverter<Delegate>
    {
        public override Delegate ReadJson(JsonReader reader, Type objectType, Delegate existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            Delegate del = null;
            while (reader.Read())
            {
                reader.Read();
                if (reader.TokenType == JsonToken.EndArray) break;
                while (reader.Value as string is not ListReferenceConverter.refProperty) reader.Read();
                reader.Read();
                var id = reader.Value as string;
                while (reader.Value as string is not "methodName") reader.Read();
                reader.Read();
                var method = reader.Value as string;
                var target = serializer.ReferenceResolver.ResolveReference(serializer, id);
                var tmpDel = Delegate.CreateDelegate(objectType, target, method);
                del = del is null ? tmpDel : Delegate.Combine(del, tmpDel);

            }
            return del;
        }

        public override void WriteJson(JsonWriter writer, Delegate value, JsonSerializer serializer)
        {
            writer.WriteStartArray();
            foreach (var invocation in value.GetInvocationList())
            {
                writer.WriteStartObject();
                writer.WritePropertyName(ListReferenceConverter.refProperty);
                if (invocation.Target is null)
                {
                    writer.WriteNull();
                }
                else
                    writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, invocation.Target));
                writer.WritePropertyName("methodName");
                writer.WriteValue(invocation.Method.Name);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
    }
}
