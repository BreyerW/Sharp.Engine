using System;
using Sharp.Editor.UI.Property;

namespace Sharp
{
    public abstract class Component
    {
        public bool active
        {
            get { return enabled; }
            set
            {
                if (enabled == value)
                    return;
                enabled = value;
                if (enabled)
                    OnEnableInternal();
                else
                    OnDisableInternal();
            }
        }

        [Range]
        public float testRange
        {
            get;
            set;
        } = 1;

        // public float[,,] testArray
        // {
        //    get;
        //    set;
        //  } = { { { 80, 45 }, { 80, 45 }, { 80, 45 } } };

        private bool enabled;
        public Entity entityObject;

        protected internal virtual void OnEnableInternal()
        {
        }

        protected internal virtual void OnDisableInternal()
        {
        }

        public Component()
        {
            active = true;
        }
    }
}