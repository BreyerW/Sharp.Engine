using System;
using Sharp;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Squid;
using Sharp.Editor;
using Fossil;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Sharp
{
    public static class Selection//enimachine engine
    {
        private static Stack<object> assets = new Stack<object>();
        private static GenericResolver<IEngineObject> refResolver = new GenericResolver<IEngineObject>(p => p.Id.ToString("N"));
        private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
        internal static byte[] lastData = Array.Empty<byte>();
        internal static byte[] lastStructure = Array.Empty<byte>();

        public static object Asset
        {
            set
            {
                if (value == Asset) return;
                assets.Push(value);
                //Thread.SetData
                OnSelectionChange?.Invoke(value, EventArgs.Empty);
            }
            get
            {
                if (assets.Count == 0)
                    return null;
                return assets.Peek();
            }
        }

        public static EventHandler OnSelectionChange;
        public static EventHandler OnSelectionDirty;
        public static bool isDragging = false;
        internal static JsonSerializer serializer;

        static Selection()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false },
                Converters = new List<JsonConverter>() { new DelegateConverter(), new ArrayReferenceConverter() },
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                TypeNameHandling = TypeNameHandling.Auto,
                //ReferenceResolverProvider = () => refResolver
            };

            serializer = JsonSerializer.CreateDefault();
            Repeat(IsSelectionDirty, 30, 30, CancellationToken.None);
        }

        public static void IsSelectionDirty(CancellationToken token)
        {
            var asset = Asset;
            if (asset == null) return;

            var data = JsonConvert.SerializeObject(asset).AsReadOnlySpan().AsBytes();

            if (!data.SequenceEqual(lastData.AsReadOnlySpan()))
            {
                UI.isDirty = true;

                if (!(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging) && !(Editor.Views.SceneView.entities is null))
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    var json = JsonConvert.SerializeObject(Editor.Views.SceneView.entities);
                    var currentStructure = json.AsReadOnlySpan().AsBytes().ToArray(); //System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(Editor.Views.SceneView.entities)); /*System.Text.Encoding.UTF8.GetBytes(*/// JSON.ToJSON(Editor.Views.SceneView.entities).AsReadOnlySpan().AsBytes().ToArray();//System.Text.Encoding.UTF8.GetBytes(JSON.ToJSON(Editor.Views.SceneView.entities));

                    watch.Stop();

                    Console.WriteLine("cast: " + watch.ElapsedMilliseconds);
                    CalculateHistoryDiff(ref currentStructure);
                    lastData = data.ToArray();
                    lastStructure = currentStructure;
                    Console.WriteLine("save");
                }
            }
        }

        private static void CalculateHistoryDiff(ref byte[] currentStructure)
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
            UndoCommand.snapshots.AddLast(new HistoryDiff() { downgrade = backward });
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
                var tmpDel = Delegate.CreateDelegate(type, serializer.ReferenceResolver.ResolveReference(serializer, invocation[0]["$ref"].Value<string>()), invocation[1].Value<string>());
                del = del is null ? tmpDel : Delegate.Combine(del, tmpDel);
            }
            Console.WriteLine(del.GetInvocationList().Length);
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
                    writer.WriteNull();
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

                    return;
                }

                writer.WriteEndObject();
            }
        }
    }
}