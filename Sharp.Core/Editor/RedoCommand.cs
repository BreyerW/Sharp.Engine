﻿using FastMember;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sharp.Editor
{
	public class RedoCommand : IMenuCommand
	{
		public string menuPath => "Redo";

		public string[] keyCombination => new[] { "CTRL", "SHIFT", "z" };

		public string Indentifier { get => menuPath; }

		// public static Stack<ICommand> undone = new Stack<ICommand>();

		public void Execute(bool reverse = false)//TODO: add object context parameter with anything under mouse?
		{
			if (UndoCommand.currentHistory.Next is null)
				return;
			UndoCommand.isUndo = false;
			var componentsToBeAdded = new Dictionary<Guid, Dictionary<string, (byte[] undo,byte[] redo)>>();
			UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			//while(UndoCommand.currentHistory.Value.propertyMapping.ContainsKey(Camera.main.GetInstanceID()))
			//	UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			UndoCommand.availableHistoryChanges = UndoCommand.currentHistory.Value.propertyMapping;
			foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (list.ContainsKey("addedEntity"))
				{
					var entity = Entity.CreateEntityForEditor();
					entity.name = Encoding.Unicode.GetString(list["addedEntity"].redo);
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
				if (list.ContainsKey("selected"))
					Selection.Asset = index.GetInstanceObject();

			}
			foreach (var (id, list) in componentsToBeAdded)
			{
				var parent = new Guid(list["Parent"].redo).GetInstanceObject<Entity>();
				var component = parent.AddComponent(RuntimeHelpers.GetUninitializedObject(Type.GetType(Encoding.Unicode.GetString(list["addedComponent"].redo))) as Component);
				if (component is Transform tr)
					parent.transform = tr;
				component.AddRestoredObject(id);
				Extension.entities.AddRestoredEngineObject(component, id);
			}
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}
	}
}