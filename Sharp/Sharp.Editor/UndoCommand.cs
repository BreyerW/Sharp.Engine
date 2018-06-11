using System;
using System.Collections.Generic;
using Sharp.Commands;
using Fossil;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Sharp.Editor
{
    public struct HistoryDiff
    {
        public byte[] downgrade;
        public byte[] upgrade;
    }

    public class UndoCommand : IMenuCommand
    {
        public static LinkedList<HistoryDiff> snapshots = new LinkedList<HistoryDiff>();

        internal static LinkedListNode<HistoryDiff> currentHistory;

        public string menuPath => "Undo";

        public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

        public string Indentifier { get => menuPath; }

        //public static Stack<ICommand> done = new Stack<ICommand>();

        public void Execute(bool reverse = true)
        {
            /*if (done.Count is 0) return;
            var toUndo = done.Peek();
            toUndo.Execute(true);
            RedoCommand.undone.Push(done.Pop());*/

            if (UndoCommand.currentHistory.Previous is null)
                return;
            var str = Delta.Apply(Selection.lastStructure, UndoCommand.currentHistory.Value.downgrade);
            Selection.lastStructure = str;
            Console.WriteLine("undo " + Views.SceneView.entities[Views.SceneView.entities.Count - 1].rotation);
            Console.WriteLine(new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()));
            //Selection.serializer.Populate(new System.IO.StringReader(new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>())), Views.SceneView.entities);
            //JsonConvert.PopulateObject(new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()), Views.SceneView.entities);//instead of sizeof should use System.Text.Encoding.Unicode.GetCharCount(str)?

            Views.SceneView.entities = JsonConvert.DeserializeObject<List<Entity>>(new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()));//instead of sizeof should use System.Text.Encoding.Unicode.GetCharCount(str)?
            currentHistory = UndoCommand.currentHistory.Previous;
            Squid.UI.isDirty = true;

            Console.WriteLine("undo " + Views.SceneView.entities[Views.SceneView.entities.Count - 1].rotation);
        }
    }
}