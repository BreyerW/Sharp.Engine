using Newtonsoft.Json;
using PluginAbstraction;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Sharp.Serializer
{
	public class JSONSerializer : ISerializerPlugin
	{
		internal static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
		{
			ContractResolver = new UninitializedResolver() { IgnoreSerializableAttribute = false },
			Converters = new List<JsonConverter>() { new DictionaryConverter(),/* new EntityConverter(),*/  new DelegateConverter(), new ArrayReferenceConverter(),/* new ListReferenceConverter(),*/ new ReferenceConverter()/* new IAssetConverter(), new IEngineConverter(), */ },
			PreserveReferencesHandling = PreserveReferencesHandling.All,
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			TypeNameHandling = TypeNameHandling.All,
			//ObjectCreationHandling = ObjectCreationHandling.Replace,
			ObjectCreationHandling = ObjectCreationHandling.Reuse,
			ReferenceResolverProvider = () => new IdReferenceResolver(),
			NullValueHandling = NullValueHandling.Ignore,
			//DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
		};
		internal static Dictionary<object, Guid> mapping = new();
		internal static Func<object, bool> isEngineObject;
		public Dictionary<object, Guid> objToIdMapping => mapping;

		public Func<object, bool> IsEngineObject { set => isEngineObject = value; }

		public object Deserialize(byte[] data, Type type)
		{
			var str = Encoding.Unicode.GetString(data);
			return JsonConvert.DeserializeObject(str, type, serializerSettings);
		}
		public byte[] Serialize(object obj, Type type)
		{
			return Encoding.Unicode.GetBytes(JsonConvert.SerializeObject(obj, type, serializerSettings));
		}
		public string GetName()
		{
			return "JsonSerializer";
		}

		public string GetVersion()
		{
			return "1.0";
		}

		public void ImportPlugins(Dictionary<string, object> plugins)
		{
			throw new NotImplementedException();
		}
	}
}
