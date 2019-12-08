using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System.Runtime.Serialization;
using System.Reflection;
using FastMember;

namespace Sharp.Editor
{
	public struct History
	{
		//public byte[] downgrade;
		//public byte[] upgrade;
		public Dictionary<Guid, Dictionary<string, string>> propertyMapping;
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
			if (UndoCommand.currentHistory.Previous is null)
				return;
			var stop = false;
			foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (list.ContainsKey("addedEntity"))
				{
					index.GetInstanceObject<Entity>()?.Destroy();
					stop = true;
				}
				else if (list.ContainsKey("removedEntity"))
				{
					//TODO: on .Net Core use RuntimeHelpers.GetUninitializedObject()
					var entity = FormatterServices.GetUninitializedObject(typeof(Entity));//pseudodeserialization thats why we use this
					stop = true;
				}
				else if (list.ContainsKey("addedComponent"))
				{
					//InspectorView.saveState.Remove(index);
					index.GetInstanceObject<Component>()?.Destroy();
					//componentsToBeAdded.Add(index.Item1, val);
					stop = true;
				}
				else if (list.ContainsKey("removedComponent"))
				{
					//componentsToBeAdded.Add(index.Item1, val);
					stop = true;
				}
				else if (list.ContainsKey("addedSystem"))
				{
					stop = true;
					throw new NotSupportedException("Systems are not implemented yet");

				}
			}
			currentHistory = UndoCommand.currentHistory.Previous;
			if (stop) return;
			foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				var obj = index.GetInstanceObject();
				if (list.ContainsKey("selected"))
					Selection.Asset = obj;
				//if (obj is Camera cam && cam == Camera.main) continue;
			}
			InspectorView.availableUndoRedo = currentHistory.Value.propertyMapping;

			Squid.UI.isDirty = true;
		}
	}
}