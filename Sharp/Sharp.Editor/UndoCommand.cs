using System;
using System.Collections.Generic;
using Sharp.Commands;

namespace Sharp.Editor
{
    public class UndoCommand : IMenuCommand
    {
        public string menuPath => "Undo";

        public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

        public string Indentifier { get => menuPath; }

        public static Stack<ICommand> done = new Stack<ICommand>();

        public void Execute(bool reverse = true)
        {
            if (done.Count is 0) return;
            var toUndo = done.Peek();
            toUndo.Execute(reverse);
            RedoCommand.undone.Push(done.Pop());
        }
    }
}