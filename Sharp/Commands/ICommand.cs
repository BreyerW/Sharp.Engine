using System;
using Sharp.Editor;

namespace Sharp.Commands
{
    public interface ICommand
    {
        //string Indentifier { get;/*set;*/}

        void Execute();
    }

    internal static class CommandHelper
    {
        public static bool IsUndo(this ICommand command)
        {
            return UndoCommand.done.Contains(command);
        }

        public static void StoreCommand(this ICommand command)
        {
            //command.Execute();
            UndoCommand.done.Push(command);
        }
    }
}