using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PluginAbstraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace JSONPlugin
{
	class UninitializedResolver : DefaultContractResolver
	{
		/*protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
		{
			var list = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						.Where(x => (x.GetCustomAttribute<NotSerializedAttribute>(true) is null && x.IsPublic) || (x.IsPrivate && x.GetCustomAttribute<SerializedAttribute>(true) is not null))
						.Select(p =>
						{
							var attr = p.GetCustomAttribute<SerializedAttribute>(true);
							return new JsonProperty()
							{
								PropertyName = p.Name,
								PropertyType = p.FieldType,
								Readable = true,
								Writable = true,
								ValueProvider = base.CreateMemberValueProvider(p),
								IsReference = attr is not null ? attr.IsReference : true
							};

						}).ToList();

			return list;
		}*/
		protected override JsonObjectContract CreateObjectContract(Type objectType)
		{
			JsonObjectContract contract = base.CreateObjectContract(objectType);
			contract.DefaultCreator = () =>
			{
				return RuntimeHelpers.GetUninitializedObject(objectType);
			};
			return contract;
		}
	}
}
