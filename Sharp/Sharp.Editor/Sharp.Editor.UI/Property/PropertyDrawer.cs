using System;
using Gwen.Skin;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

namespace Gwen.Control.Property
{
    /// <summary>
    /// Base control for property entry.
    /// </summary>
    public abstract class PropertyDrawer<T> : Control.Base//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDrawer"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public PropertyDrawer(Control.Base parent) : base(parent)
        {
            Height = 17;
        }

        /// <summary>
        /// Invoked when the property value has been changed.
        /// </summary>
		public event GwenEventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Property value (todo: always string, which is ugly. do something about it).
        /// </summary>
		public virtual T Value { get { return default(T); } set { SetValue(value, false); } }

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public virtual bool IsEditing { get { return false; } }

        protected virtual void DoChanged()
        {
            if (ValueChanged != null)
                ValueChanged.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnValueChanged(Control.Base control, EventArgs args)
        {
            DoChanged();
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="fireEvents">Determines whether to fire "value changed" event.</param>
        public virtual void SetValue(T value, bool fireEvents = false)
        {
        }
    }

    public abstract class PropertyDrawer<T, BindToAttribute> : PropertyDrawer<T> where BindToAttribute : Sharp.Editor.UI.Property.CustomPropertyDrawerAttribute //change to interface so that could support color case?
    {
        public BindToAttribute attribute;

        public PropertyDrawer(Base parent) : base(parent)
        {
        }
    }

    /*public abstract class ArrayDrawer<T> : PropertyDrawer<T> where T : IEnumerable
    {
        public ArrayDrawer(Base parent) : base(parent)
        {
        }
    }

    public abstract class ArrayDrawer<T, BindToAttribute> : ArrayDrawer<T> where T : IEnumerable where BindToAttribute : Sharp.Editor.UI.Property.CustomPropertyDrawerAttribute
    {
        public ArrayDrawer(Base parent) : base(parent)
        {
        }
    }

    public class arrayTest : ArrayDrawer<int[]>
    {
        public arrayTest(Base parent) : base(parent)
        {
        }
    }*/
}