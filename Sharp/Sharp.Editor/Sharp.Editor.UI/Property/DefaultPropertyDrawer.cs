using System;
using System.Collections.Generic;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI.Property
{
    internal class DefaultPropertyDrawer : PropertyDrawer<object>
    {
        public override object Value { get { return null; } set { } }

        public DefaultPropertyDrawer(Base parent) : base(parent)
        {
        }
    }
}