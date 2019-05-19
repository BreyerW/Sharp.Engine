using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Serialization;

namespace Sharp.Editor
{
	public struct History
	{
		//public byte[] downgrade;
		//public byte[] upgrade;
		public Dictionary<(Guid, string), string> propertyMapping;
		public bool onlyAdditionOrSubtraction;
		//public Guid? selectedObject;
	}

	public class UndoCommand : IMenuCommand
	{
		internal static LinkedList<History> snapshots = new LinkedList<History>();

		internal static LinkedListNode<History> currentHistory;

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
			//lock (Selection.sync)
			//{
			if (UndoCommand.currentHistory.Previous is null)
				return;
			Console.WriteLine("undo" + UndoCommand.currentHistory.Previous.Value.propertyMapping);
			/*var undid=Selection.memStream.GetStream();
			//Delta.Apply(Selection.lastStructure, UndoCommand.currentHistory.Value.downgrade,undid);
			//Selection.lastStructure.Dispose();
			//Selection.lastStructure = undid;
			//Console.WriteLine("undo " + SceneView.entities.root[SceneView.entities.root.Count - 1].ModelMatrix);
			//Console.WriteLine(System.Text.Encoding.Default.GetString(str)); /*new string(Unsafe.As<byte[], char[]>(ref str), 0, str.Length / Unsafe.SizeOf<char>()));*
			var serializer = JsonSerializer.CreateDefault();
			using (var sr = new StreamReader(Selection.lastStructure, System.Text.Encoding.UTF8, true, 4096, true))
			using (var jsonReader = new JsonTextReader(sr))
			{
				jsonReader.ArrayPool = JsonArrayPool.Instance;
				serializer.Populate(jsonReader, Extension.entities);
				//SceneView.entities = serializer.Deserialize<Root>(jsonReader);
			}*/
			currentHistory = UndoCommand.currentHistory.Previous;

			//Console.WriteLine("only addition? " + UndoCommand.currentHistory.Value.onlyAdditionOrSubtraction);
			if (UndoCommand.currentHistory.Value.onlyAdditionOrSubtraction)
			{
				InspectorView.availableUndoRedo = currentHistory.Value.propertyMapping;
				foreach (var ((index, keyword), val) in UndoCommand.currentHistory.Value.propertyMapping)
				{
					if (keyword == "addedEntity")
					{
						index.GetInstanceObject<Entity>()?.Destroy();
					}
					else if (keyword == "removedEntity")
					{
						//TODO: on .Net Core use RuntimeHelpers.GetUninitializedObject()
						var entity = FormatterServices.GetUninitializedObject(typeof(Entity));//pseudodeserialization thats why we use this
					}
					else if (keyword == "addedComponent")
					{
						index.GetInstanceObject<Component>()?.Destroy();
						//componentsToBeAdded.Add(index.Item1, val);
					}
					else if (keyword == "removedComponent")
					{
						//componentsToBeAdded.Add(index.Item1, val);
					}
					else if (keyword == "addedSystem")
					{
						throw new NotSupportedException("Systems are not implemented yet");
					}
				}
				InspectorView.availableUndoRedo = null;
			}
			else
				InspectorView.availableUndoRedo = currentHistory.Value.propertyMapping;
			//var componentsToBeAdded = new Dictionary<Guid, string>();

			Squid.UI.isDirty = true;
		}
	}
}