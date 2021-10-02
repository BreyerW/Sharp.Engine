using Sharp.Editor.UI.Property;
using System;

namespace Sharp.Editor
{
    public abstract class ComponentDrawer : ComponentNode
    {
        private Component target;
        public abstract void OnInitializeGUI();
        public virtual bool CanApply(Type type) => true;
        public Component Target
        {
            get => target;
            internal set
            {
                target = value;
                UserData = value;
            }
        }
    }
    public abstract class ComponentDrawer<T> : ComponentDrawer where T : Component//SelectionDrawer
    {
    }
}