using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;
using FastMember;

namespace Sharp.Editor
{
	public struct History
	{
		//public byte[] downgrade;
		//public byte[] upgrade;
		public Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>> propertyMapping;
		//public Guid? selectedObject;
	}

	public class UndoCommand : IMenuCommand
	{
		internal static LinkedList<History> snapshots = new LinkedList<History>();

		internal static LinkedListNode<History> currentHistory;
		internal static Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>> availableHistoryChanges;
		internal static bool isUndo = false;
		public string menuPath => "Undo";

		public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

		public string Indentifier { get => menuPath; }

		//public static Stack<ICommand> done = new Stack<ICommand>();

		public void Execute(bool reverse = true)
		{
			if (UndoCommand.currentHistory.Previous is null)
				return;
			isUndo = true;
			//while (UndoCommand.currentHistory.Value.propertyMapping.ContainsKey(Camera.main.GetInstanceID()))
			//	UndoCommand.currentHistory = UndoCommand.currentHistory.Previous;
			foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (list.ContainsKey("addedEntity"))
				{
					index.GetInstanceObject<Entity>()?.Destroy();
				}
				else if (list.ContainsKey("removedEntity"))
				{
					//TODO: on .Net Core use RuntimeHelpers.GetUninitializedObject()
					var entity = RuntimeHelpers.GetUninitializedObject(typeof(Entity));//pseudodeserialization thats why we use this

				}
				else if (list.ContainsKey("addedComponent"))
				{
					//InspectorView.saveState.Remove(index);
					index.GetInstanceObject<Component>()?.Destroy();
					//componentsToBeAdded.Add(index.Item1, val);

				}
				else if (list.ContainsKey("removedComponent"))
				{
					//componentsToBeAdded.Add(index.Item1, val);

				}
				else if (list.ContainsKey("addedSystem"))
				{
					throw new NotSupportedException("Systems are not implemented yet");

				}
				if (list.ContainsKey("selected"))
				{
					var obj = list["selected"].undo is null ? null : new Guid(list["selected"].undo).GetInstanceObject();
					Selection.Asset = obj;

				}
			}

			//foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{


				//if (obj is Camera cam && cam == Camera.main) continue;
			}

			UndoCommand.availableHistoryChanges = currentHistory.Value.propertyMapping;
			currentHistory = UndoCommand.currentHistory.Previous;
			Squid.UI.isDirty = true;
		}
	}
}