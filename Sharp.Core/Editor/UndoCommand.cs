using Fossil;
//using Microsoft.Collections.Extensions;
using Sharp.Core;
using Sharp.Engine.Components;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp.Editor
{
	public class UndoCommand : IMenuCommand
	{
		public string menuPath => "Undo";
		public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

		public string Indentifier { get => menuPath; }

		//public static Stack<ICommand> done = new Stack<ICommand>();

		public void Execute(bool reverse = true)
		{
			if (History.current.Previous is null)
				return;
			History.isUndo = true;

			foreach (var (index, undo, redo) in History.current.Value.propertyMapping)
			{

				var obj = index.GetInstanceObject<IEngineObject>();
				if (undo is not null)
				{
					ref var patched = ref CollectionsMarshal.GetValueRefOrNullRef(History.prevStates, obj);
					patched = Delta.Apply(patched, undo);
					var str = Encoding.Unicode.GetString(patched);
					PluginManager.serializer.Deserialize(patched, obj.GetType());
					if (obj is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
						startable.Start();
				}
				else
				{
					History.prevStates.Remove(obj);
					obj.Dispose();
				}
			}

			Coroutine.AdvanceInstructions<WaitForUndoRedo>();
			History.current = History.current.Previous;
			Squid.UI.isDirty = true;
		}
	}
}