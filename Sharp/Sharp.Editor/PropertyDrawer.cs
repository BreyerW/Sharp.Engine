using System;
using System.Collections.Generic;
using Gwen.Control.Property;
using Gwen.Control;

namespace Sharp.Editor
{
    public abstract class PropertyDrawer<T>
    {
        public abstract void OnInitializeGUI(Properties propertyTree, string propertyName, object propertyValue, Action<object> setter);
    }
}
