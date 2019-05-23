using Newtonsoft.Json;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

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
			if (UndoCommand.currentHistory.Next is null)
				return;
			if (UndoCommand.currentHistory.Value.onlyAdditionOrSubtraction || UndoCommand.currentHistory.Next.Value.onlyAdditionOrSubtraction)
			{
				var componentsToBeAdded = new Dictionary<Guid, (string, Guid)>();
				UndoCommand.currentHistory = UndoCommand.currentHistory.Value.onlyAdditionOrSubtraction ? UndoCommand.currentHistory : UndoCommand.currentHistory.Next;
				InspectorView.availableUndoRedo = UndoCommand.currentHistory.Value.propertyMapping;
				foreach (var ((index, keyword), val) in InspectorView.availableUndoRedo)
				{
					if (keyword == "addedEntity")
					{
						Entity.undoRedoContext = true;
						var entity = new Entity();
						Entity.undoRedoContext = false;
						entity.name = val;
						entity.AddRestoredObject(index);
						Extension.entities.AddRestoredEngineObject(entity, index);
					}
					else if (keyword == "addedComponent")
					{
						componentsToBeAdded.Add(index, (val, new Guid(UndoCommand.currentHistory.Value.propertyMapping[(index, "Parent")])));
					}
					else if (keyword == "addedSystem")
					{
						throw new NotSupportedException("Systems are not implemented yet");
					}

				}
				foreach (var (id, (type, guid)) in componentsToBeAdded)
				{
					var parent = guid.GetInstanceObject<Entity>();
					Component.undoRedoContext = true;
					var component = parent.AddComponent(Type.GetType(type));
					Component.undoRedoContext = false;
					component.AddRestoredObject(id);
					Extension.entities.AddRestoredEngineObject(component, id);
				}
			}
			UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			InspectorView.availableUndoRedo = UndoCommand.currentHistory.Value.propertyMapping;
			Squid.UI.isDirty = true;
		}
	}
}