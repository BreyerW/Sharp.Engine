using FastMember;
using Fossil;
using Newtonsoft.Json;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using System;
using System.Collections.Generic;
using System.Linq;
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
					var entity = Entity.CreateEntityForEditor();
					entity.name = Encoding.Unicode.GetString(redo);
					entity.AddRestoredObject(index);
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
					ref var patched = ref UndoCommand.prevStates.GetOrAddValueRef(index.GetInstanceObject<Component>());
					var objToBePatched = Encoding.Unicode.GetBytes(patched);
					patched = Encoding.Unicode.GetString(Delta.Apply(objToBePatched, redo));
					JsonConvert.DeserializeObject(patched, index.GetInstanceObject<Component>().GetType(), MainClass.serializerSettings);
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
				//Extension.objectToIdMapping.TryGetValue(componentsToBeAdded.Keys[0],out _);
				var str = Encoding.Unicode.GetString(list.redo);
				var component = JsonConvert.DeserializeObject(str,Type.GetType(Encoding.Unicode.GetString(list.undo)), MainClass.serializerSettings) as Component;
				component.Parent.AddComponent(component);
				UndoCommand.prevStates.GetOrAddValueRef(component) = str;
				//component.AddRestoredObject(id);
				Extension.entities.AddRestoredEngineObject(component, id);
			}
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}
	}
}