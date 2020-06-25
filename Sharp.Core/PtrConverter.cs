using Newtonsoft.Json;
using Sharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp
{
	public class PtrConverter : JsonConverter<Ptr>//TODO: add tracking same intptrs do alloc only once rest ignore
	{
		public override Ptr ReadJson(JsonReader reader, Type objectType, [AllowNull] Ptr existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (hasExistingValue && !existingValue.IsFreed)
				return existingValue;
			return new Ptr((IntPtr)(long)reader.Value);
		}

		public override void WriteJson(JsonWriter writer, [AllowNull] Ptr value, JsonSerializer serializer)
		{
			unsafe
			{
				var len = value.Length;
				writer.WriteValue(IntPtr.Size is 32 ? len.ToInt32() : len.ToInt64());
			}
		}
	}
}
