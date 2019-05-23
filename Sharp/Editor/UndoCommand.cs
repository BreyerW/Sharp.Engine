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
			if (UndoCommand.currentHistory.Previous is null)
				return;
			currentHistory = UndoCommand.currentHistory.Previous;

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