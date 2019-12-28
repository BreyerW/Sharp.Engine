using System;
using Sharp.Commands;

namespace Sharp.Editor
{
    public interface IMenuCommand : ICommand
    {
        string menuPath { get; }
        string[] keyCombination { get; }
    }
}