using Fossil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sharp.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Linq;
using System.Collections;
using System.Runtime.CompilerServices;
using SharpAsset;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Sharp.Engine.Components;

namespace Sharp
{
	public static class Selection//enimachine engine
	{
		private static Stack<object> assets = new Stack<object>();


		internal static Stream lastStructure;// memStream.GetStream();
											 //private static Root cachedRoot = new Root();
		public static object sync = new object();

		public static object Asset
		{
			set
			{
				if (value == Asset) return;
				OnSelectionChange?.Invoke(Asset, value);
				if (value is null)
					assets.Clear();
				else
					assets.Push(value);
				//Thread.SetData

			}
			get
			{
				if (assets.Count == 0)
					return null;
				return assets.Peek();
			}
		}

		internal static string tempPrevName = Path.GetTempFileName();
		internal static string tempCurrName = Path.GetTempFileName();
		public static Action<object, object> OnSelectionChange;
		public static Action<object> OnSelectionDirty;
		public static bool isDragging = false;
		//internal static JsonSerializer serializer;

		static Selection()
		{
			//memStream.AggressiveBufferReturn = true;

			//lastStructure = memStream.GetStream();// new FileStream(tempPrevName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096);
		}

		/*try
				{
					serializer = JsonSerializer.CreateDefault();
					using (var sw = new StreamWriter(tempName, false))
					{
						using (var jsonWriter = new JsonTextWriter(sw))
						{
							serializer.Serialize(jsonWriter, Editor.Views.SceneView.entities);
						}
					}
				}
				catch (IOException io)
				{
					Console.WriteLine(io.Message);
				}
				tempFile = new FileStream(tempName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 2048, options: FileOptions.Asynchronous | FileOptions.SequentialScan);

				var tmpdata = JsonConvert.SerializeObject(Editor.Views.SceneView.entities);
				var data = tmpdata.AsReadOnlySpan().AsBytes();
				if (tempFile.Length == tmpdata.Length)
				{
					for (int i = 0; i < tempFile.Length; i++)
					{
						if (tmpdata[i] != tempFile.ReadByte())
						{
							Console.WriteLine("not identical.");
							break;
						}
					}

					Console.WriteLine("identical");
				}
				else Console.WriteLine("not identical " + tempFile.Length + " " + tmpdata.Length);
				tempFile.Close();*/

		public static void IsSelectionDirty(CancellationToken token)
		{//produce component drawers and hide/detach them when not active for all object in scene and calculate diff for each property separately
			
		}

		

		//static ManualResetEventSlim waiter = new ManualResetEventSlim();
		public static async Task Repeat(Action<CancellationToken> doWork, int delayInMilis, int periodInMilis, CancellationToken cancellationToken, bool singleThreaded = false)
		{
			await Task.Delay(delayInMilis, cancellationToken).ConfigureAwait(singleThreaded);
			while (!cancellationToken.IsCancellationRequested)
			{
				//waiter.Wait(delayInMilis, cancellationToken);
				doWork(cancellationToken);
				//waiter.Reset();
				await Task.Delay(periodInMilis, cancellationToken).ConfigureAwait(singleThreaded);
			}
		}

		public static void RemoveAllBefore<T>(this LinkedListNode<T> node)
		{
			while (node.Previous != null) node.List.Remove(node.Previous);
		}

		public static void RemoveAllAfter<T>(this LinkedListNode<T> node)
		{
			while (node.Next != null) node.List.Remove(node.Next);
		}
	}

	public class DelegateConverter : JsonConverter<MulticastDelegate>
	{
		public override MulticastDelegate ReadJson(JsonReader reader, Type objectType, MulticastDelegate existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			//if (!hasExistingValue) return null;
			var tokens = JToken.Load(reader);
			if (!tokens.HasValues) return null;
			//var type = tokens["signature"].ToObject<Type>();
			Delegate del = null;
			//Console.WriteLine("declTYpe:" + existingValue.GetType().DeclaringType);
			foreach (var invocation in tokens["invocations"])
			{
				//Console.WriteLine("deserialized: " + serializer.Deserialize(invocation[0].CreateReader()));
				var tmpDel = Delegate.CreateDelegate(objectType, /*serializer.Deserialize(invocation[0].CreateReader())*/ serializer.ReferenceResolver.ResolveReference(serializer, (invocation[0]["$ref"] ?? invocation[0]["$id"]).Value<string>()), invocation[1].Value<string>());
				del = del is null ? tmpDel : Delegate.Combine(del, tmpDel);
			}
			return del as MulticastDelegate;
		}

		public override void WriteJson(JsonWriter writer, MulticastDelegate value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			//writer.WritePropertyName("signature");
			//serializer.Serialize(writer, value.GetType());
			//Console.WriteLine(value.GetType());
			writer.WritePropertyName("invocations");
			writer.WriteStartArray();
			foreach (var invocation in value.GetInvocationList())
			{
				writer.WriteStartArray();
				if (invocation.Target is null)
				{
					writer.WriteNull();
					Console.WriteLine("null written");
				}
				else
					serializer.Serialize(writer, invocation.Target);
				writer.WriteValue(invocation.Method.Name);

				writer.WriteEndArray();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
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
	public class ReferenceConverter : JsonConverter
	{
		public override bool CanWrite => false;
		public override bool CanConvert(Type objectType)
		{
			return !objectType.IsValueType && !typeof(IList).IsAssignableFrom(objectType) && !typeof(Delegate).IsAssignableFrom(objectType) && !typeof(MulticastDelegate).IsAssignableFrom(objectType);
		}
		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var id = reader.ReadAsString();
			var obj = serializer.ReferenceResolver.ResolveReference(serializer, id);

			if (obj is null)
			{
				obj = serializer.ContractResolver.ResolveContract(objectType).DefaultCreator();
				serializer.ReferenceResolver.AddReference(serializer, id,obj);
				//obj.AddRestoredObject(new Guid(id));
			}
			//var members = objectType.GetTypeInfo().GetMembers();//TODO: add private support
			ReadOnlySpan<char> name = "";
			int dotPos = -1;
			if (reader.TokenType == JsonToken.String)
			{
				dotPos = reader.Path.LastIndexOf('.');
				if(dotPos >-1)
				name = reader.Path.AsSpan()[0..dotPos];

			}
				while (reader.Read())
			{
				if (reader.Path.AsSpan().SequenceEqual(name)) break;
				if (reader.TokenType == JsonToken.PropertyName)
				{
					var member = objectType.GetMember(reader.Value as string);
					if (member.Length is 0) continue;
						reader.Read();
					
						if (member[0] is PropertyInfo p)
						{
							p.SetValue(obj, serializer.Deserialize(reader, p.PropertyType));
						}
						else if (member[0] is FieldInfo f)
						{
							f.SetValue(obj, serializer.Deserialize(reader, f.FieldType));
						}
				}
			}
			return obj;
		}
		public static JsonReader CopyReaderForObject(JsonReader reader, JToken jToken)
		{
			JsonReader jTokenReader = jToken.CreateReader();
			jTokenReader.Culture = reader.Culture;
			jTokenReader.DateFormatString = reader.DateFormatString;
			jTokenReader.DateParseHandling = reader.DateParseHandling;
			jTokenReader.DateTimeZoneHandling = reader.DateTimeZoneHandling;
			jTokenReader.FloatParseHandling = reader.FloatParseHandling;
			jTokenReader.MaxDepth = reader.MaxDepth;
			jTokenReader.SupportMultipleContent = reader.SupportMultipleContent;
			return jTokenReader;
		}
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
	//TODO: use IEngineObject for engine references and use listreferenceconverter and DelegateConverter for list and delegates but other references wont be supported ?
	//TODO: removing component that doesnt exist after selection changed, smoothing out scenestructure rebuild after redo/undo, fix bug with ispropertydirty, add transform component
	public class IdReferenceResolver : IReferenceResolver//TODO: if nothing else works try custom converter with CanConvert=>value.IsReferenceType;
	{
		internal readonly IDictionary<Guid, object> _idToObjects = new Dictionary<Guid, object>();
		internal readonly IDictionary<object, Guid> _objectsToId = new Dictionary<object, Guid>();
		//Resolves $ref during deserialization
		public object ResolveReference(object context, string reference)
		{
			var id = new Guid(reference);//.ToByteArray();

			var o = Extension.objectToIdMapping.FirstOrDefault((obj) => obj.Value == id).Key;
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
			return _objectsToId.ContainsKey(value);
		}
		//Resolves $id during deserialization
		public void AddReference(object context, string reference, object value)
		{
			if (value.GetType().IsValueType) return;
			Guid anotherId = new Guid(reference);
			//Extension.objectToIdMapping.TryGetValue(value, out var id);
			Extension.objectToIdMapping.TryAdd(value, anotherId);
			_idToObjects[anotherId] = value;
			_objectsToId[value] = anotherId;
		}
	}

	public class ListReferenceConverter : JsonConverter
	{
		internal const string refProperty = "$ref";
		internal const string idProperty = "$id";
		internal const string valuesProperty = "$values";
		public override bool CanConvert(Type objectType)
		{
			return typeof(IList).IsAssignableFrom(objectType); // && !objectType.IsArray;
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			var obj = JToken.Load(reader);
			//if (obj is JValue) return null;
			//for (var i = 0; i < obj.Type == JTokenType.Array ? obj.Children().Count() : 1; i++)

			var refId = objectType.IsArray ? null : (string)obj[refProperty] ?? (string)obj[idProperty]; //?? (string)obj[idProperty] was added because we need persistence between Serialize() calls and it is possible that one object can be serialized in multiple places with $id rather than $ref
			if (refId != null)
			{
				var reference = serializer.ReferenceResolver.ResolveReference(serializer, refId);
				if (reference != null)
				{
					using (JsonReader jObjectReader = ReferenceConverter.CopyReaderForObject(reader, obj))
					{
						serializer.Populate(jObjectReader, reference);
					}
					return reference;//TODO: populate before returning?
				}
			}
			//Console.WriteLine("exist: " + existingValue);
			var values = (obj.Type == JTokenType.Array ? obj : obj[valuesProperty]) as JArray;
			if (values is null || values.Type == JTokenType.Null)
				return null;
			var count = values.Count;
			var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GetGenericArguments()[0];

			var value = Array.CreateInstance(elementType, count) as IList;
			if (!objectType.IsArray)
				value = Activator.CreateInstance(objectType, value) as IList;
			var objId = /*objectType.IsArray ? null :*/ (string)obj[idProperty];
			if (objId != null)
			{
				// Add the empty array into the reference table BEFORE populating it,
				// to handle recursive references.
				var refObj = serializer.ReferenceResolver.ResolveReference(serializer, objId) as IList;
				if (refObj is null)
					serializer.ReferenceResolver.AddReference(serializer, objId, value);
				else
				{
					foreach(var i in ..refObj.Count)
						refObj[i] = obj[valuesProperty][i].ToObject(elementType, JsonSerializer.Create(MainClass.serializerSettings));
					return refObj;
				}
			}
			int id = 0;
			foreach (var token in values)
			{
				value[id] = serializer.Deserialize(token.CreateReader(), elementType);
				id++;
			}
			existingValue = value;
			existingValue.AddRestoredObject(new Guid(objId));
			return existingValue;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (value is IList array)
			{
				writer.WriteStartObject();
				if (!serializer.ReferenceResolver.IsReferenced(serializer, value))
				{
					writer.WritePropertyName(idProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
					writer.WritePropertyName(valuesProperty);
					writer.WriteStartArray();
					foreach (var item in array)
					{
						serializer.Serialize(writer, item);
					}
					writer.WriteEndArray();
				}
				else
				{
					writer.WritePropertyName(refProperty);
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				}
				writer.WriteEndObject();
			}
		}
	}

	public class JsonArrayPool : IArrayPool<char>

	{
		public static readonly JsonArrayPool Instance = new JsonArrayPool();

		public char[] Rent(int minimumLength)
		{
			return ArrayPool<char>.Shared.Rent(minimumLength);
		}

		public void Return(char[] array)
		{
			ArrayPool<char>.Shared.Return(array);
		}
	}
}

/*public class ArrayReferenceConverter : JsonConverter

{
	public override bool CanConvert(Type objectType)
	{
		return objectType.IsArray;
	}

	public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
			return null;
		else if (reader.TokenType == JsonToken.StartArray)
		{
			// No $ref.  Deserialize as a List<T> to avoid infinite recursion and return as an array.
			var elementType = objectType.GetElementType();
			//if (existingValue is null)
			{
				var listType = typeof(List<>).MakeGenericType(elementType);
				var list = serializer.Deserialize(reader, listType) as IList;
				if (list == null)
					return null;

				existingValue = Array.CreateInstance(elementType, list.Count);
				list.CopyTo((Array)existingValue, 0);
			}
			/*else
			{
				Console.WriteLine("exist: " + existingValue);
			}*
			return existingValue;
		}
		else
		{
			var obj = JObject.Load(reader);
			var refId = (string)obj[refProperty];
			if (refId != null)
			{
				var reference = serializer.ReferenceResolver.ResolveReference(serializer, refId);
				if (reference != null)
					return reference;
			}
			//Console.WriteLine("exist: " + existingValue);
			var values = obj[valuesProperty] as JArray;
			if (values == null || values.Type == JTokenType.Null)
				return null;
			var count = values.Count;

			var elementType = objectType.GetElementType();
			var array = Array.CreateInstance(elementType, count);

			var objId = (string)obj[idProperty];
			if (objId != null)
			{
				// Add the empty array into the reference table BEFORE populating it,
				// to handle recursive references.
				serializer.ReferenceResolver.AddReference(serializer, objId, array);
			}

			var listType = typeof(List<>).MakeGenericType(elementType);
			using (var subReader = values.CreateReader())
			{
				var list = serializer.Deserialize(subReader, listType) as IList;
				list.CopyTo(array, 0);
			}

			return array;
		}
	}

	public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
	{
		if (value is Array array)
		{
			writer.WriteStartObject();
			if (!serializer.ReferenceResolver.IsReferenced(serializer, value))
			{
				writer.WritePropertyName(idProperty);
				writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				writer.WritePropertyName(valuesProperty);
				writer.WriteStartArray();
				foreach (var item in array)
				{
					serializer.Serialize(writer, item);
				}
				writer.WriteEndArray();
			}
			else
			{
				writer.WritePropertyName(refProperty);
				writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
			}
			writer.WriteEndObject();
		}
	}
}*/
