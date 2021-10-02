using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JSONPlugin
{
    //TODO: use IEngineObject for engine references and use listreferenceconverter and DelegateConverter for list and delegates but other references wont be supported ?
    //TODO: removing component that doesnt exist after selection changed, smoothing out scenestructure rebuild after redo/undo, fix bug with ispropertydirty, add transform component
    public class IdReferenceResolver : IReferenceResolver//TODO: if nothing else works try custom converter with CanConvert=>value.IsReferenceType;
    {
        internal readonly IDictionary<Guid, object> _idToObjects = new Dictionary<Guid, object>();
        internal readonly IDictionary<object, Guid> _objectsToId = new Dictionary<object, Guid>();
        private object rootObj = null;
        private bool rootAlreadyChecked = false;

        public readonly static Dictionary<Guid, Type> guidToTypeMapping = new();

        //Resolves $ref during deserialization
        public object ResolveReference(object context, string reference)
        {
            var id = new Guid(reference);//.ToByteArray();
            var map = JSONSerializer.mapping;
            var o = map.FirstOrDefault((obj) => obj.Value == id).Key;
            if (o is null)
            {
                o = RuntimeHelpers.GetUninitializedObject(guidToTypeMapping[id]);
                if (JSONSerializer.isEngineObject(o))
                    Extension.AddRestoredObject(emptyComp, id);
                Extension.entities.AddRestoredEngineObject(emptyComp, id);
            }
            //_idToObjects.TryGetValue(new Guid(reference), out var o);
            return o;
        }
        //Resolves $id or $ref value during serialization
        public string GetReference(object context, object value)
        {
            if (value.GetType().IsValueType) return null;
            //if (!Extension.objectToIdMapping.TryGetValue(value, out var id))
            if (!_objectsToId.TryGetValue(value, out var id))
            {
                id = value.GetInstanceID();//.ToByteArray(); //Guid.NewGuid().ToByteArray(); 
                AddReference(context, id.ToString(), value);
            }
            if (guidToTypeMapping.ContainsKey(id) is false)
                guidToTypeMapping.Add(id, value.GetType());
            return id.ToString();//value.GetInstanceID().ToString();//
        }
        //Resolves if $id or $ref should be used during serialization
        public bool IsReferenced(object context, object value)
        {
            if (rootAlreadyChecked is false && JSONSerializer.isEngineObject(value))
            {
                rootAlreadyChecked = true;
                rootObj = value;
                return false;
            }
            rootAlreadyChecked = true;
            return rootObj is not null && JSONSerializer.isEngineObject(value) ? true : _objectsToId.ContainsKey(value);
        }
        //Resolves $id during deserialization
        public void AddReference(object context, string reference, object value)
        {
            if (value.GetType().IsValueType) return;
            Guid anotherId = new Guid(reference);
            JSONSerializer.mapping.TryAdd(value, anotherId);
            _idToObjects[anotherId] = value;
            _objectsToId.TryAdd(value, anotherId);
        }
    }
    static class Extensions
    {
        public static Guid GetInstanceID<T>(this T obj) where T : class
        {
            if (!JSONSerializer.mapping.TryGetValue(obj, out var id))
            {
                JSONSerializer.mapping.Add(obj, id = Guid.NewGuid());
                //throw new InvalidOperationException("attempted to add new entity this shouldnt be happening");
            }
            return id;
        }
    }
}
/*
 public class EntityConverter : JsonConverter<Entity>
	{
		public override bool CanRead => false;

		public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ListReferenceConverter.refProperty);
			writer.WriteValue(value.GetInstanceID());
			writer.WriteEndObject();
		}
	}


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
 */
