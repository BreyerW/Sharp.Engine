using Fossil;
using Sharp.Core;
using Sharp.Engine.Components;
using Sharp.Serializer;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sharp.Editor
{
	public class RedoCommand : IMenuCommand
	{
		public string menuPath => "Redo";

		public string[] keyCombination => new[] { "CTRL", "SHIFT", "z" };

		public string Indentifier { get => menuPath; }

		public void Execute(bool reverse = false)//TODO: add object context parameter with anything under mouse?
		{
			if (UndoCommand.currentHistory.Next is null)
				return;
			UndoCommand.historyMoved = true;
			UndoCommand.isUndo = false;
			var componentsToBeAdded = new Dictionary<Guid, (byte[] undo, byte[] redo)>();
			UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			foreach (var (index, (label, undo, redo)) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (label is "addedEntity")
				{
					var entity = PluginManager.serializer.Deserialize(redo, typeof(Entity)) as Entity;

					entity.components = new List<Component>();
					entity.childs = new List<Entity>();
					UndoCommand.prevStates.GetOrAddValueRef(entity) = redo;
					Extension.entities.AddRestoredEngineObject(entity, index);
				}
				else if (label is "addedComponent")
				{
					componentsToBeAdded.Add(index, (undo, redo));
				}
				else if (label is "addedSystem")
				{
					throw new NotSupportedException("Systems are not implemented yet");
				}
				else if (label is "changed")
				{
					ref var patched = ref UndoCommand.prevStates.GetOrAddValueRef(index.GetInstanceObject<IEngineObject>());
					patched = Delta.Apply(patched, redo);
					var obj = index.GetInstanceObject<IEngineObject>();
					PluginManager.serializer.Deserialize(patched, obj.GetType());
					if (obj is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
						startable.Start();
					//Console.WriteLine(object.ReferenceEquals(index.GetInstanceObject(),UndoCommand.prevStates.FirstOrDefault((obj) => obj.Key.GetInstanceID() == index).Key));
				}
				if (label is "selected")
				{
					var obj = redo is null ? null : new Guid(redo).GetInstanceObject();
					Selection.Asset = obj;
				}
			}
			foreach (var (id, _) in componentsToBeAdded)
			{
				var emptyComp = RuntimeHelpers.GetUninitializedObject(IdReferenceResolver.guidToTypeMapping[id]) as Component;
				Extension.AddRestoredObject(emptyComp, id);
				Extension.entities.AddRestoredEngineObject(emptyComp, id);
			}
			foreach (var (id, list) in componentsToBeAdded)
			{
				var component = PluginManager.serializer.Deserialize(list.redo, IdReferenceResolver.guidToTypeMapping[id]) as Component;
				component.Parent.components.Add(component);
				UndoCommand.prevStates.GetOrAddValueRef(component) = list.redo;
			}
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}
	}
}