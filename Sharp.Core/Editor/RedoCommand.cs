using Fossil;
using Sharp.Core;
using System;
using System.Collections.Generic;
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
			//while(UndoCommand.currentHistory.Value.propertyMapping.ContainsKey(Camera.main.GetInstanceID()))
			//	UndoCommand.currentHistory = UndoCommand.currentHistory.Next;
			//UndoCommand.availableHistoryChanges = UndoCommand.currentHistory.Value.propertyMapping;
			foreach (var (index, (label, undo, redo)) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (label is "addedEntity")
				{
					var component = PluginManager.serializer.Deserialize(redo, typeof(Entity)) as Entity;

					component.components = new List<Component>();
					component.childs = new List<Entity>();
					UndoCommand.prevStates.GetOrAddValueRef(component) = redo;
					//component.AddRestoredObject(index);
					Extension.entities.AddRestoredEngineObject(component, index);
					/*var entity = Entity.CreateEntityForEditor();
					entity.name = Encoding.Unicode.GetString(redo);
					entity.AddRestoredObject(index);
					Extension.entities.AddRestoredEngineObject(entity, index);*/
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
					PluginManager.serializer.Deserialize(patched, index.GetInstanceObject<IEngineObject>().GetType());
					//Console.WriteLine(object.ReferenceEquals(index.GetInstanceObject(),UndoCommand.prevStates.FirstOrDefault((obj) => obj.Key.GetInstanceID() == index).Key));
				}
				if (label is "selected")
				{
					var obj = redo is null ? null : new Guid(redo).GetInstanceObject();
					Selection.Asset = obj;
				}
			}
			foreach (var (id, list) in componentsToBeAdded)
			{
				var component = PluginManager.serializer.Deserialize(list.redo, Type.GetType(Encoding.Unicode.GetString(list.undo))) as Component;
				component.Parent.components.Add(component);
				UndoCommand.prevStates.GetOrAddValueRef(component) = list.redo;
				//component.AddRestoredObject(id);
				Extension.entities.AddRestoredEngineObject(component, id);

				//TODO: theres bug with not adding component back to entitys internal collection
			}
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}
	}
}