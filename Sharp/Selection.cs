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
				assets.Push(value);
				//Thread.SetData
				OnSelectionChange?.Invoke(value);
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
		public static Action<object> OnSelectionChange;
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
			/*if ((InputHandler.isKeyboardPressed | InputHandler.isMouseDragging))//&& !(Editor.Views.SceneView.entities is null)
			{
				//Squid.UI.isDirty = true;
				return;
			}
			lock (sync)
			{
				//var tmpdata = JsonConvert.SerializeObject(Editor.Views.SceneView.entities);
				//var data = tmpdata.AsReadOnlySpan().AsBytes();
				/*using (var sw = new StreamWriter(tempName, false))
			{
					using (var jsonWriter = new JsonTextWriter(sw))
					{
						serializer.Serialize(jsonWriter, Editor.Views.SceneView.entities);
					}
				}*
				//var mem = new FileStream(tempCurrName, FileMode.Truncate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096);
				var serializer = JsonSerializer.CreateDefault();
				var mem = memStream.GetStream();

				using (var sw = new StreamWriter(mem, System.Text.Encoding.UTF8, 4096, true))//
				using (var jsonWriter = new JsonTextWriter(sw))
				{
					var watch = System.Diagnostics.Stopwatch.StartNew();
					//sw.AutoFlush = true;
					jsonWriter.ArrayPool = JsonArrayPool.Instance;
					serializer.Serialize(jsonWriter, Editor.Views.SceneView.entities);

					watch.Stop();
					//Console.WriteLine("cast: " + watch.ElapsedMilliseconds);
				}
				mem.Position = 0;
				lastStructure.Position = 0;
				var condition = mem.Length == lastStructure.Length;
				//Console.WriteLine("serializedarr: " + System.Text.Encoding.UTF8.GetString(mem.ToArray()));
				for (int i = 0, j = 0; j < lastStructure.Length && i < mem.Length && condition; i++, j++)
				{
					var b1 = lastStructure.ReadByte();
					var b2 = mem.ReadByte();
					//Console.WriteLine(b1 + " " + b2);
					condition = !((b1 is -1 && b2 > -1) || (b2 is -1 && b1 > -1) || b1 != b2);
				}
				if (!condition)
				{
					//Console.WriteLine("current: " + tmpdata);
					//Console.WriteLine("past: " + new string(Unsafe.As<byte[], char[]>(ref lastStructure), 0, lastStructure.Length / Unsafe.SizeOf<char>()));
					Squid.UI.isDirty = true;


					//System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Editor.Views.SceneView.entities)); /*System.Text.Encoding.UTF8.GetBytes(// JSON.ToJSON(Editor.Views.SceneView.entities).AsReadOnlySpan().AsBytes().ToArray();//System.Text.Encoding.UTF8.GetBytes(JSON.ToJSON(Editor.Views.SceneView.entities));

					//CalculateHistoryDiff(mem);
					lastStructure.Dispose();
					lastStructure = mem;
					//Utils.Swap(ref tempCurrName, ref tempPrevName);
					Console.WriteLine("save");//problemem jest camera jej zmiany triggeruja zapis
				}
				else
					mem.Dispose();
			}*/
		}

		/*private static void CalculateHistoryDiff(Stream currentStructure)
		{
			currentStructure.Position = 0;
			var backward = Delta.Create(currentStructure, lastStructure);

			if (!(UndoCommand.currentHistory is null))
			{
				var forward = Delta.Create(lastStructure, currentStructure);

				if (UndoCommand.currentHistory != UndoCommand.snapshots.Last)
					UndoCommand.currentHistory.RemoveAllAfter();

				var copy = UndoCommand.snapshots.Last.Value;
				copy.upgrade = forward;
				UndoCommand.snapshots.Last.Value = copy;
			}
			Console.WriteLine("data size: " + currentStructure.Length + " patch size: " + backward.Length);
			UndoCommand.snapshots.AddLast(new HistoryDiff() { downgrade = backward, selectedObject = Asset?.GetInstanceID() });
			UndoCommand.currentHistory = UndoCommand.snapshots.Last;
		}
		*/

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

	//TODO: removing component that doesnt exist after selection changed, smoothing out scenestructure rebuild after redo/undo, fix bug with ispropertydirty, add transform component
	public class ThreadsafeReferenceResolver : IReferenceResolver
	{

		//private static IDictionary<string, string> referenceToRoot = new Dictionary<string, string>();

		//private static Dictionary<string, WeakReference<object>> guidToObjMapping = new Dictionary<string, WeakReference<object>>();
		private IDictionary<string, object> stringToReference;

		private IDictionary<object, string> referenceToString;

		//private int referenceCount = 0;

		public ThreadsafeReferenceResolver()
		{
			stringToReference = new Dictionary<string, object>(EqualityComparer<string>.Default);
			referenceToString = new Dictionary<object, string>(EqualityComparer<object>.Default);
		}

		public void AddReference(
			object context,
			string reference,
			object value)
		{
			if (value.GetType().IsValueType) return;
			if (stringToReference.TryGetValue(reference, out _))
				return;
			referenceToString.Add(value, reference);
			stringToReference.Add(reference, value);
		}

		public string GetReference(
			object context,
			object value)
		{
			if (value.GetType().IsValueType) return null;
			if (!referenceToString.TryGetValue(value, out string result))
			{
				//Console.WriteLine(value + " " + rootRef);
				result = value.GetInstanceID().ToString();
				AddReference(context, result, value);
			}

			return result;
		}

		public bool IsReferenced(
			object context,
			object value)
		{
			//var isReferenced = objToGuidMapping.TryGetValue(value, out var reference);
			//Console.WriteLine(isReferenced + " " + rootRef);
			//isReferenced = isReferenced && ((string.IsNullOrEmpty(rootRef)) /*&& (referenceToRoot.ContainsKey(reference) && referenceToRoot[reference] != rootRef)*/);

			return referenceToString.TryGetValue(value, out _);
		}

		public object ResolveReference(
			object context,
			string reference)
		{
			stringToReference.TryGetValue(reference, out var obj);
			return obj;
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
			var refId = (string)obj[refProperty] ?? (string)obj[idProperty]; //?? (string)obj[idProperty] was added because we need persistence between Serialize() calls and it is possible that one object can be serialized in multiple places with $id rather than $ref
			if (refId != null)
			{
				var reference = serializer.ReferenceResolver.ResolveReference(serializer, refId);
				if (reference != null)
					return reference;
			}
			//Console.WriteLine("exist: " + existingValue);
			var values = (obj.Type == JTokenType.Array ? obj : obj[valuesProperty]) as JArray;
			if (values == null || values.Type == JTokenType.Null)
				return null;
			var count = values.Count;
			var elementType = objectType.IsArray ? objectType.GetElementType() : objectType.GetGenericArguments()[0];

			var value = Array.CreateInstance(elementType, count) as IList;
			if (!objectType.IsArray)
				value = Activator.CreateInstance(objectType, value) as IList;
			var objId = (string)obj[idProperty];
			if (objId != null)
			{
				// Add the empty array into the reference table BEFORE populating it,
				// to handle recursive references.
				serializer.ReferenceResolver.AddReference(serializer, objId, value);
			}
			int id = 0;
			foreach (var token in values)
			{
				value[id] = serializer.Deserialize(token.CreateReader(), elementType);
				id++;
			}

			existingValue = value;

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
