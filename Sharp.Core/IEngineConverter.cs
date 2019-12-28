using Newtonsoft.Json;
using System;

namespace Sharp
{
	class IEngineConverter : JsonConverter<IEngineObject>
	{
		public override IEngineObject ReadJson(JsonReader reader, Type objectType, IEngineObject existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			//reader.Read();
			return new Guid(reader.Value as string).GetInstanceObject<IEngineObject>();
		}

		public override void WriteJson(JsonWriter writer, IEngineObject value, JsonSerializer serializer)
		{
			writer.WriteValue(value.GetInstanceID());
		}
	}
}
