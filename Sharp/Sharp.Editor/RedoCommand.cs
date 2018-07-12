using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Runtime.CompilerServices;
using System.IO;

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
			lock (Selection.sync)
			{
				if (UndoCommand.currentHistory.Next is null)
					return;

				/*Console.WriteLine("redo " + SceneView.entities.root[SceneView.entities.root.Count - 1].rotation);*/
				Delta.Apply(Selection.lastStructure, UndoCommand.currentHistory.Value.upgrade);

				var serializer = JsonSerializer.CreateDefault();
				using (var sr = new StreamReader(Selection.lastStructure, System.Text.Encoding.UTF8, true, 4096, true))
				using (var jsonReader = new JsonTextReader(sr))
				{
					SceneView.entities = serializer.Deserialize<Root>(jsonReader);
				}

				Selection.Asset = UndoCommand.currentHistory.Value.selectedObject.HasValue ? SceneView.entities.allEngineObjects[UndoCommand.currentHistory.Value.selectedObject.Value] : null;
				UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
				Squid.UI.isDirty = true;
				/*Console.WriteLine("redo " + SceneView.entities.root[SceneView.entities.root.Count - 1].rotation); */
			}
		}
	}
}