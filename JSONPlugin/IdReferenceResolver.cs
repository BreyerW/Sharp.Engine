using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

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
		//Resolves $ref during deserialization
		public object ResolveReference(object context, string reference)
		{
			var id = new Guid(reference);//.ToByteArray();
			var map = JSONSerializer.mapping();
			var o = map.FirstOrDefault((obj) => obj.Value == id).Key;
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
			return id.ToString();//value.GetInstanceID().ToString();//
		}
		//Resolves if $id or $ref should be used during serialization
		public bool IsReferenced(object context, object value)
		{
			/*if (rootAlreadyChecked is false && value.GetType().IsClass)
			{
				rootObj = value;
				return false;
			}
			rootAlreadyChecked = true;*/
			return rootObj is not null ? true : _objectsToId.ContainsKey(value);
		}
		//Resolves $id during deserialization
		public void AddReference(object context, string reference, object value)
		{
			if (value.GetType().IsValueType) return;
			Guid anotherId = new Guid(reference);
			//Extension.objectToIdMapping.TryGetValue(value, out var id);
			var map = JSONSerializer.mapping();
			map.TryAdd(value, anotherId);
			_idToObjects[anotherId] = value;
			_objectsToId.TryAdd(value, anotherId);
		}
	}
	static class Extensions
	{
		public static Guid GetInstanceID<T>(this T obj) where T : class
		{
			if (!JSONSerializer.mapping().TryGetValue(obj, out var id))
			{
				JSONSerializer.mapping().Add(obj, id = Guid.NewGuid());
				//throw new InvalidOperationException("attempted to add new entity this shouldnt be happening");
			}
			return id;
		}
	}
}
