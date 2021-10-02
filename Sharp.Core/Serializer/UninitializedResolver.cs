using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Sharp.Serializer
{
    class UninitializedResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            Stack<Type> hierarchy = new();
            do
            {
                hierarchy.Push(type);
                type = type.BaseType;
            } while (type is not null);
            Dictionary<string, MemberInfo> members = new();
            while (hierarchy.TryPop(out var t))
            {
                foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(x => x is FieldInfo fi && ((fi.GetCustomAttribute<JsonIgnoreAttribute>(true) is null && fi.IsPublic) || (fi.IsPrivate && x.GetCustomAttribute<JsonIncludeAttribute>(true) is not null))))
                    members.TryAdd(f.Name, f);

                foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(x =>
                            x is PropertyInfo pi && ((pi.GetCustomAttribute<JsonIgnoreAttribute>(true) is null && pi.GetSetMethod(true) is { } mi && mi.IsPublic)
                            || (pi.GetSetMethod(true) is { } pmi && pmi.IsPrivate && x.GetCustomAttribute<JsonIncludeAttribute>(true) is not null))))

                    members.TryAdd(p.Name, p);
            }
            var list = members.Select(p =>
               {
                   //var attr = p.GetCustomAttribute<JsonIncludeAttribute>(true);
                   return new JsonProperty()
                   {
                       PropertyName = p.Key,
                       PropertyType = p.Value is FieldInfo fi ? fi.FieldType : (p.Value as PropertyInfo).PropertyType,
                       Readable = true,
                       Writable = true,
                       ValueProvider = base.CreateMemberValueProvider(p.Value),
                       IsReference = true// attr is not null ? attr.IsReference : true
                   };

               }).ToList();
            return list;
        }
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
