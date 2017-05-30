using System;
using System.Collections.Generic;
using Sharp.Editor;

namespace Sharp.Commands
{
    internal class ChangeValueCommand : ICommand
    {
        private readonly Action<object> _setValueAction;
        private readonly object _originalValue;
        public object newValue;

        public ChangeValueCommand(Action<object> setValueAction, object originalValue)
        {
            _setValueAction = setValueAction;
            _originalValue = originalValue;
        }

        public ChangeValueCommand(Action<object> setValueAction, object originalValue, object value) : this(setValueAction, originalValue)
        {
            newValue = value;
        }

        public void Execute()
        {
            if (this.IsUndo())
                _setValueAction(_originalValue);
            else _setValueAction(newValue);
        }
    }
}