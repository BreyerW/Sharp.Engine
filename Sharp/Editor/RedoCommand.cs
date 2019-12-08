using FastMember;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
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

			var componentsToBeAdded = new Dictionary<Guid, Dictionary<string, string>>();
			UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			InspectorView.availableUndoRedo = UndoCommand.currentHistory.Value.propertyMapping;
			foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (list.ContainsKey("addedEntity"))
				{
					var entity = Entity.CreateEntityForEditor();
					entity.name = list["addedEntity"];
					entity.AddRestoredObject(index);
					Extension.entities.AddRestoredEngineObject(entity, index);
				}
				else if (list.ContainsKey("addedComponent"))
				{
					componentsToBeAdded.Add(index, list);
				}
				else if (list.ContainsKey("addedSystem"))
				{
					throw new NotSupportedException("Systems are not implemented yet");
				}
				else if (list.ContainsKey("selected"))
					Selection.Asset = index.GetInstanceObject();

			}
			foreach (var (id, list) in componentsToBeAdded)
			{
				var parent = new Guid(list["Parent"]).GetInstanceObject<Entity>();
				var component = parent.AddComponent(FormatterServices.GetUninitializedObject(Type.GetType(list["addedComponent"])) as Component);
				if (component is Transform tr)
					parent.transform = tr;
				//InspectorView.availableUndoRedo.Add(id, list);//block anything related to main camera?
				component.AddRestoredObject(id);
				Extension.entities.AddRestoredEngineObject(component, id);
			}
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set
			Squid.UI.isDirty = true;
		}
	}
}