using System;
using System.Collections.Generic;

namespace PluginAbstraction
{
	public interface ISerializerPlugin : IPlugin
	{
		Func<Dictionary<object, Guid>> objToIdMapping { set; }
		byte[] Serialize(object obj, Type type);
		object Deserialize(byte[] data, Type type);
	}
}
