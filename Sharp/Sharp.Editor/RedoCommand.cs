using System;
using System.Collections.Generic;
using Sharp.Commands;
using Fossil;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace Sharp.Editor
{
    public class RedoCommand : IMenuCommand
    {
        public string menuPath => "Redo";

        public string[] keyCombination => new[] { "CTRL", "SHIFT", "z" };

        public string Indentifier { get => menuPath; }

        // public static Stack<ICommand> undone = new Stack<ICommand>();

        public void Execute(bool reverse = false)
        {
            /* if (undone.Count is 0) return;
             var toRedo = undone.Pop();
             toRedo.Execute(false);
             UndoCommand.done.Push(toRedo);*/
            if (UndoCommand.currentHistory.Next is null)
                return;
            Console.WriteLine("redo " + Views.SceneView.entities[Views.SceneView.entities.Count - 1].rotation);
            var str = Delta.Apply(Selection.lastStructure, UndoCommand.currentHistory.Value.upgrade);
            Selection.lastStructure = str;
            Views.SceneView.entities = JsonConvert.DeserializeObject<List<Entity>>(new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()));
            UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
            Console.WriteLine("redo " + Views.SceneView.entities[Views.SceneView.entities.Count - 1].rotation);
        }
    }
}