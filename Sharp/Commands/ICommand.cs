using System;
using Sharp.Editor;

namespace Sharp.Commands
{
    public interface ICommand
    {
        //string Indentifier { get;/*set;*/}

        void Execute(bool reverse = false);
    }

    internal static class CommandHelper
    {
        public static void StoreCommand(this ICommand command)
        {
            //command.Execute();
            UndoCommand.done.Push(command);
        }
    }
}