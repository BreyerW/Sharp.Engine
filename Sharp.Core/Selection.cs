using Fossil;
using Newtonsoft.Json;
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
using System.Runtime.InteropServices;
using System.Linq;
using OpenTK.Graphics.ES11;
using SharpAvi.Codecs;

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

		static Selection()
		{
			//memStream.AggressiveBufferReturn = true;

			//lastStructure = memStream.GetStream();// new FileStream(tempPrevName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096);
		}



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

	public class DelegateConverter : JsonConverter<Delegate>
	{
		public override Delegate ReadJson(JsonReader reader, Type objectType, Delegate existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			Delegate del = null;
			while (reader.Read())
			{
				reader.Read();
				if (reader.TokenType == JsonToken.EndArray) break;
				while (reader.Value as string is not ListReferenceConverter.refProperty) reader.Read();
				reader.Read();
				var id = reader.Value as string;
				while (reader.Value as string is not "methodName") reader.Read();
				reader.Read();
				var method = reader.Value as string;
				var tmpDel = Delegate.CreateDelegate(objectType, serializer.ReferenceResolver.ResolveReference(serializer, id), method);
				del = del is null ? tmpDel : Delegate.Combine(del, tmpDel);

			}
			return del;
		}

		public override void WriteJson(JsonWriter writer, Delegate value, JsonSerializer serializer)
		{
			writer.WriteStartArray();
			foreach (var invocation in value.GetInvocationList())
			{
				writer.WriteStartObject();
				writer.WritePropertyName(ListReferenceConverter.refProperty);
				if (invocation.Target is null)
				{
					writer.WriteNull();
				}
				else
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, invocation.Target));
				writer.WritePropertyName("methodName");
				writer.WriteValue(invocation.Method.Name);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
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
	public class DictionaryConverter : JsonConverter<IDictionary>
	{

		public override IDictionary ReadJson(JsonReader reader, Type objectType, IDictionary existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var id = reader.ReadAsString();
			var obj = serializer.ReferenceResolver.ResolveReference(serializer, id) as IDictionary;

			if (obj is null)
			{
				obj = serializer.ContractResolver.ResolveContract(objectType).DefaultCreator() as IDictionary;
				serializer.ReferenceResolver.AddReference(serializer, id, obj);
			}
			obj.Clear();
			ReadOnlySpan<char> name = "";
			int dotPos = -1;
			if (reader.TokenType == JsonToken.String)
			{
				dotPos = reader.Path.LastIndexOf('.');
				if (dotPos > -1)
					name = reader.Path.AsSpan()[0..dotPos];
			}
			while (reader.Read())
			{
				if (reader.Path.AsSpan().SequenceEqual(name)) break;
				if (reader.TokenType == JsonToken.EndArray /*&& reader.Value as string is "pairs"*/)
					break;
				while (reader.Value as string is not "key") reader.Read();
				reader.Read();
				var generics = objectType.GetGenericArguments();
				var key = serializer.Deserialize(reader, generics[0]);
				while (reader.Value as string is not "value") reader.Read();
				reader.Read();
				var value = serializer.Deserialize(reader, generics[1]);
				obj.Add(key, value);
				reader.Read();
			}
			return obj;
		}

		public override void WriteJson(JsonWriter writer, IDictionary value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ListReferenceConverter.idProperty);
			writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
			writer.WritePropertyName("pairs");
			writer.WriteStartArray();
			foreach (DictionaryEntry item in value)
			{
				writer.WriteStartObject();
				writer.WritePropertyName("key");
				serializer.Serialize(writer, item.Key);
				writer.WritePropertyName("value");
				serializer.Serialize(writer, item.Value);
				writer.WriteEndObject();
			}
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
	}
	public class ReferenceConverter : JsonConverter
	{
		public override bool CanWrite => false;
		public override bool CanConvert(Type objectType)
		{
			return objectType != typeof(string) && !objectType.IsValueType && !typeof(IList).IsAssignableFrom(objectType) && !typeof(Delegate).IsAssignableFrom(objectType) && !typeof(MulticastDelegate).IsAssignableFrom(objectType);
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
				serializer.ReferenceResolver.AddReference(serializer, id, obj);
			}
			ReadOnlySpan<char> name = "";
			int dotPos = -1;
			if (reader.TokenType == JsonToken.String)
			{
				dotPos = reader.Path.LastIndexOf('.');
				if (dotPos > -1)
					name = reader.Path.AsSpan()[0..dotPos];
			}
			while (reader.Read())
			{
				if (reader.Path.AsSpan().SequenceEqual(name)) break;
				if (reader.TokenType == JsonToken.PropertyName)
				{
					var member = objectType.GetMember(reader.Value as string, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
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

	public class ListReferenceConverter : JsonConverter<IList>
	{
		internal const string refProperty = "$ref";
		internal const string idProperty = "$id";
		internal const string valuesProperty = "$values";


		public override IList ReadJson(JsonReader reader, Type objectType, IList existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			reader.Read();
			var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GetGenericArguments()[0];
			var refId = reader.ReadAsString();

			while (reader.TokenType is not JsonToken.StartArray) reader.Read();
			reader.Read();
			if (refId != null)
			{
				IList tmp = new List<object>();
				while (true)
				{
					tmp.Add(serializer.Deserialize(reader, elementType));
					reader.Read();
					if (reader.TokenType is JsonToken.EndArray) { reader.Read(); break; }
					while (!elementType.IsPrimitive && reader.TokenType is not JsonToken.StartObject) reader.Read();
				}
				var reference = serializer.ReferenceResolver.ResolveReference(serializer, refId) as IList;
				if (reference is not null)
				{
					if (objectType.IsArray)
					{
						foreach (var i in ..reference.Count)
							reference[i] = tmp[i];
						return reference;
					}
					else
					{
						reference.Clear();
						existingValue = reference;
					}
				}
				else
					existingValue = Activator.CreateInstance(objectType, tmp.Count) as IList;

				foreach (var i in ..tmp.Count)
					if (reference is not null && !objectType.IsArray)
						existingValue.Add(tmp[i]);
					else
						existingValue[i] = tmp[i];
				serializer.ReferenceResolver.AddReference(serializer, refId, existingValue);
				return existingValue;
			}

			throw new NotSupportedException();
		}

		public override void WriteJson(JsonWriter writer, IList value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			if (!serializer.ReferenceResolver.IsReferenced(serializer, value))
			{
				writer.WritePropertyName(idProperty);
				writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				writer.WritePropertyName(valuesProperty);
				writer.WriteStartArray();
				foreach (var item in value)
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