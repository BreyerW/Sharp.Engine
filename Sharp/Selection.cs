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

namespace Sharp
{
    public static class Selection//enimachine engine
    {
        private static Stack<object> assets = new Stack<object>();

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

        static Selection()
        {
            Repeat(IsSelectionDirty, 30, 30, CancellationToken.None);
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>() { new DelegateConverter() },
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize
            };
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
            //Console.WriteLine("nowy typek: " + existingValue.Method);
            var tokens = JToken.Load(reader);
            Console.WriteLine(tokens);
            return existingValue;
        }

        public override void WriteJson(JsonWriter writer, MulticastDelegate value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("target");
            serializer.Serialize(writer, value.Target);
            writer.WritePropertyName("signature");
            serializer.Serialize(writer, value.Method);
            writer.WritePropertyName("invocations");
            writer.WriteStartArray();
            foreach (var invocation in value.GetInvocationList())
            {
                writer.WriteStartArray();
                serializer.Serialize(writer, invocation.Target);
                serializer.Serialize(writer, invocation.Method);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
            /*writer.WriteStartObject();
             writer.WritePropertyName("mainTarget");
             writer.WriteValue(serializer.ReferenceResolver.GetReference(value, value.Target));
             writer.WriteEndObject();*/
        }

        //public override bool CanWrite => false;
    }
}