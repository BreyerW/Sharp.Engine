using Newtonsoft.Json;
using SharpAsset;
using SharpAsset.Pipeline;
using System;

namespace Sharp
{
	class IAssetConverter : JsonConverter<IAsset>
	{
		public override IAsset ReadJson(JsonReader reader, Type objectType, IAsset existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return Pipeline.assetToPipelineMapping[objectType].Import(reader.Value as string);
		}

		public override void WriteJson(JsonWriter writer, IAsset value, JsonSerializer serializer)
		{
			//writer.WriteStartObject();
			writer.WriteValue(value.FullPath);
			//writer.WriteEndObject();
		}
	}
}
