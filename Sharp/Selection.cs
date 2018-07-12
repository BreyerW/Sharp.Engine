using Fossil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sharp.Editor;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp
{
	public static class Selection//enimachine engine
	{
		private static Stack<object> assets = new Stack<object>();

		private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
		{
			ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false },
			Converters = new List<JsonConverter>() { new DelegateConverter(), new ArrayReferenceConverter() },
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
			PreserveReferencesHandling = PreserveReferencesHandling.All,
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			TypeNameHandling = TypeNameHandling.All,
			ObjectCreationHandling = ObjectCreationHandling.Auto,
			ReferenceResolverProvider = () => new ThreadsafeReferenceResolver()
		};

		private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
		internal static MemoryStream lastStructure = new MemoryStream();
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
		internal static JsonSerializer serializer;

		static Selection()
		{
			JsonConvert.DefaultSettings = () => serializerSettings;
			serializer = JsonSerializer.CreateDefault();
			Repeat(IsSelectionDirty, 30, 30, CancellationToken.None);
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
		{
			lock (sync)
			{
				var asset = Asset;
				if (asset == null) return;
				//var tmpdata = JsonConvert.SerializeObject(Editor.Views.SceneView.entities);
				//var data = tmpdata.AsReadOnlySpan().AsBytes();
				/*using (var sw = new StreamWriter(tempName, false))
				{
					using (var jsonWriter = new JsonTextWriter(sw))
					{
						serializer.Serialize(jsonWriter, Editor.Views.SceneView.entities);
					}
				}*/
				var tempFile = new FileStream(tempCurrName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 2048, options: FileOptions.Asynchronous);
				serializer = JsonSerializer.CreateDefault();
				var mem = memStream.GetStream();
				using (var sw = new StreamWriter(mem, System.Text.Encoding.UTF8, 4096, true))//
				using (var jsonWriter = new JsonTextWriter(sw))
				{
					//sw.AutoFlush = true;
					serializer.Serialize(jsonWriter, Editor.Views.SceneView.entities);

					/*for (int i = 0; i < mem.Length; i++)
					{
						var b = mem.ReadByte();
						//Console.WriteLine(b);
						if (b != -1 && tmpdata[i] != b)
						{
							//Console.WriteLine("not identical." + mem.Length + " " + data.Length + " " + tmpdata.Length);
							break;
						}
						//else Console.WriteLine("identical." + mem.Length + " " + data.Length + " " + tmpdata.Length);
					}
					*/
					//tempFile.Close();
					//mem.Seek(0, SeekOrigin.Begin);
				}
				//Console.WriteLine(lastStructure);
				var data = mem.ToArray();
				//Console.WriteLine(data.Array);
				if (!data.AsReadOnlySpan().SequenceEqual(lastStructure.ToArray().AsReadOnlySpan()))
				{
					//Console.WriteLine("current: " + tmpdata);
					//Console.WriteLine("past: " + new string(Unsafe.As<byte[], char[]>(ref lastStructure), 0, lastStructure.Length / Unsafe.SizeOf<char>()));
					//UI.isDirty = true;

					if (!(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging) && !(Editor.Views.SceneView.entities is null))
					{
						var watch = System.Diagnostics.Stopwatch.StartNew();
						var currentStructure = data; //System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Editor.Views.SceneView.entities)); /*System.Text.Encoding.UTF8.GetBytes(// JSON.ToJSON(Editor.Views.SceneView.entities).AsReadOnlySpan().AsBytes().ToArray();//System.Text.Encoding.UTF8.GetBytes(JSON.ToJSON(Editor.Views.SceneView.entities));
						watch.Stop();

						Console.WriteLine("cast: " + watch.ElapsedMilliseconds);
						CalculateHistoryDiff(mem);
						lastStructure = mem;
						Console.WriteLine("save");
					}
				}
			}
		}

		private static void CalculateHistoryDiff(MemoryStream currentStructure)
		{
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
			UndoCommand.snapshots.AddLast(new HistoryDiff() { downgrade = backward, selectedObject = (Asset as IEngineObject)?.Id });
			UndoCommand.currentHistory = UndoCommand.snapshots.Last;
		}

		public static async Task Repeat(Action<CancellationToken> doWork, int delayInMilis, int periodInMilis, CancellationToken cancellationToken, bool singleThreaded = false)
		{
			await Task.Delay(delayInMilis, cancellationToken).ConfigureAwait(singleThreaded);
			while (!cancellationToken.IsCancellationRequested)
			{
				doWork(cancellationToken);
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
			var tokens = JToken.Load(reader);
			var type = tokens["signature"].ToObject<Type>();
			Delegate del = null;
			foreach (var invocation in tokens["invocations"])
			{
				Console.WriteLine("deserialized: " + serializer.Deserialize(invocation[0].CreateReader()));
				var tmpDel = Delegate.CreateDelegate(type, /*serializer.Deserialize(invocation[0].CreateReader())*/ serializer.ReferenceResolver.ResolveReference(serializer, (invocation[0]["$ref"] ?? invocation[0]["$id"]).Value<string>()), invocation[1].Value<string>());
				del = del is null ? tmpDel : Delegate.Combine(del, tmpDel);
			}
			//Console.WriteLine(del.GetInvocationList().Length);
			return del as MulticastDelegate;
		}

		public override void WriteJson(JsonWriter writer, MulticastDelegate value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("signature");
			serializer.Serialize(writer, value.GetType());
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

	public class GenericResolver<TEntity> : IReferenceResolver where TEntity : class
	{
		private readonly IDictionary<string, TEntity> _objects = new Dictionary<string, TEntity>();
		private readonly Func<TEntity, string> _keyReader;

		public GenericResolver(Func<TEntity, string> keyReader)
		{
			_keyReader = keyReader;
		}

		public object ResolveReference(object context, string reference)
		{
			_objects.TryGetValue(reference, out var o);
			return o;
		}

		public string GetReference(object context, object value)
		{
			var o = (TEntity)value;
			var key = _keyReader(o);
			_objects[key] = o;

			return key;
		}

		public bool IsReferenced(object context, object value)
		{
			var o = (TEntity)value;
			return _objects.ContainsKey(_keyReader(o));
		}

		public void AddReference(object context, string reference, object value)
		{
			if (value is TEntity val)
				_objects[reference] = val;
		}
	}

	//TODO: removing component that doesnt exist after selection changed, smoothing out scenestructure rebuild after redo/undo, fix bug with ispropertydirty, add transform component
	public class ThreadsafeReferenceResolver : IReferenceResolver
	{
		private IDictionary<string, object> stringToReference;
		private IDictionary<object, string> referenceToString;
		private int referenceCount = 0;

		public ThreadsafeReferenceResolver()
		{
			this.stringToReference = new Dictionary<string, object>(EqualityComparer<string>.Default);
			this.referenceToString = new Dictionary<object, string>(EqualityComparer<object>.Default);
		}

		public void AddReference(
			object context,
			string reference,
			object value)
		{
			if (value.GetType().IsValueType) return;
			/*if (referenceToString.TryGetValue(value, out var existingSecond))
			 {
				 if (!existingSecond.Equals(reference) || ( ))
				 {
					 return; //throw new ArgumentException("reference duplication with different instances on " + reference);
				 }
			 }*/
			if (stringToReference.TryGetValue(reference, out var existingFirst))
			{
				if (!existingFirst.Equals(value) && value is IEngineObject obj1 && existingFirst is IEngineObject obj2 && obj1.Id != obj2.Id)
				{
					throw new ArgumentException("reference duplication with different instances on " + value);
				}
			}
			this.referenceToString.Add(value, reference);
			this.stringToReference.Add(reference, value);
		}

		public string GetReference(
			object context,
			object value)
		{
			if (!this.referenceToString.TryGetValue(value, out string result))
			{
				result = referenceCount.ToString(CultureInfo.InvariantCulture);
				AddReference(context, result, value);
				Interlocked.Increment(ref referenceCount);
			}

			return result;
		}

		public bool IsReferenced(
			object context,
			object value)
		{
			return this.referenceToString.ContainsKey(value);
		}

		public object ResolveReference(
			object context,
			string reference)
		{
			this.stringToReference.TryGetValue(reference, out var r);
			return r;
		}
	}

	public class ArrayReferenceConverter : JsonConverter

	{
		private const string refProperty = "$ref";
		private const string idProperty = "$id";
		private const string valuesProperty = "$values";

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
				var listType = typeof(List<>).MakeGenericType(elementType);
				var list = serializer.Deserialize(reader, listType) as System.Collections.IList;
				if (list == null)
					return null;
				var array = Array.CreateInstance(elementType, list.Count);
				list.CopyTo(array, 0);
				return array;
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
					var list = serializer.Deserialize(subReader, listType) as System.Collections.IList;
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
					writer.WritePropertyName("$id");
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
					writer.WritePropertyName("$values");
					writer.WriteStartArray();
					foreach (var item in array)
					{
						serializer.Serialize(writer, item);
					}
					writer.WriteEndArray();
				}
				else
				{
					writer.WritePropertyName("$ref");
					writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				}
				writer.WriteEndObject();
			}
		}
	}
}