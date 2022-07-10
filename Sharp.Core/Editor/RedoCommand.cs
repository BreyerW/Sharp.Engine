using Fossil;
using Sharp.Core;
using Sharp.Engine.Components;
using Sharp.Serializer;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
			if (History.current.Next is null)
				return;
			History.isUndo = false;

			History.current = History.current.Next;
			foreach (var (index, undo, redo) in History.current.Value.propertyMapping)
			{
				var str = Encoding.Unicode.GetString(redo);
				if (undo is not null)
				{
					var obj = index.GetInstanceObject<IEngineObject>();
					ref var patched = ref CollectionsMarshal.GetValueRefOrNullRef(History.prevStates, obj);
					patched = Delta.Apply(patched, redo);

					PluginManager.serializer.Deserialize(patched, obj.GetType());
					//Console.WriteLine(object.ReferenceEquals(index.GetInstanceObject(),UndoCommand.prevStates.FirstOrDefault((obj) => obj.Key.GetInstanceID() == index).Key));
				}
				else
				{
					var component = PluginManager.serializer.Deserialize(redo, IdReferenceResolver.guidToTypeMapping[index]) as IEngineObject;
					CollectionsMarshal.GetValueRefOrAddDefault(History.prevStates, component, out _) = redo;
					if (component is Entity)
						Extension.entities.AddRestoredEngineObject(component, index);
					if (component is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
						startable.Start();
				}
			}

			Coroutine.AdvanceInstructions<WaitForUndoRedo>();
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}

	}
}