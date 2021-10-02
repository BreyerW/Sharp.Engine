using Sharp.Commands;
using System;

namespace Sharp.Editor
{
    public interface IMenuCommand : ICommand
    {
        string menuPath { get; }
        string[] keyCombination { get; }
    }
}