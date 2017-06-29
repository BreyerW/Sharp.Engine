using System;
using System.Collections.Generic;
using Sharp.Commands;

namespace Sharp.Editor
{
    public class RedoCommand : IMenuCommand
    {
        public string menuPath => "Redo";

        public string[] keyCombination => new[] { "CTRL", "SHIFT", "z" };

        public string Indentifier { get => menuPath; }

        public static Stack<ICommand> undone = new Stack<ICommand>();

        public void Execute(bool reverse = false)
        {
            if (undone.Count is 0) return;
            var toRedo = undone.Pop();
            toRedo.Execute(reverse);
            UndoCommand.done.Push(toRedo);
        }
    }
}