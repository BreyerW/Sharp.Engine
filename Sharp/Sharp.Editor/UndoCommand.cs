using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;

namespace Sharp.Editor
{
	public struct HistoryDiff
	{
		public byte[] downgrade;
		public byte[] upgrade;
		public Guid? selectedObject;
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
			lock (Selection.sync)
			{
				if (UndoCommand.currentHistory.Previous is null)
					return;

				Delta.Apply(Selection.lastStructure, UndoCommand.currentHistory.Value.downgrade);

				//Console.WriteLine("undo " + SceneView.entities.root[SceneView.entities.root.Count - 1].ModelMatrix);
				//Console.WriteLine(System.Text.Encoding.Default.GetString(str)); /*new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()));*/
				var serializer = JsonSerializer.CreateDefault();
				using (var sr = new StreamReader(Selection.lastStructure, System.Text.Encoding.UTF8, true, 4096, true))
				using (var jsonReader = new JsonTextReader(sr))
				{
					SceneView.entities = serializer.Deserialize<Root>(jsonReader);
				}
				Selection.Asset = UndoCommand.currentHistory.Previous.Value.selectedObject.HasValue ? SceneView.entities.allEngineObjects[UndoCommand.currentHistory.Previous.Value.selectedObject.Value] : null;//TODO: kolejnosc selectow zbugowana?
				currentHistory = UndoCommand.currentHistory.Previous;
				Squid.UI.isDirty = true;

				//Console.WriteLine("undo " + Views.SceneView.entities.root[SceneView.entities.root.Count - 1].ModelMatrix);
			}
		}
	}
}