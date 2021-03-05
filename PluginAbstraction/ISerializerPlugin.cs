using System;
using System.Collections.Generic;

namespace PluginAbstraction
{
	public interface ISerializerPlugin : IPlugin
	{
		Dictionary<object, Guid> objToIdMapping { get; }
		Func<object, bool> IsEngineObject { set; }
		byte[] Serialize(object obj, Type type);
		object Deserialize(byte[] data, Type type);
	}
}
