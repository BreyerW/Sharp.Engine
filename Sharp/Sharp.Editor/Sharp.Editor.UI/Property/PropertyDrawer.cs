using System;
using Sharp.Commands;
using Sharp.Editor.Attribs;

namespace Gwen.Control.Property
{
    /// <summary>
    /// Base control for property entry.
    /// </summary>
    public abstract class PropertyDrawer<T> : Base//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
    {
        public Action<T> setter;
        public Func<T> getter;
        public CustomPropertyDrawerAttribute[] attributes;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyDrawer"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public PropertyDrawer(Control.Base parent) : base(parent)
        {
            Height = 17;
        }

        /// <summary>
        /// Property value (todo: always string, which is ugly. do something about it).
        /// </summary>
		public abstract T Value
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public virtual bool IsEditing { get { return false; } }

        protected void OnValueChanged(Control.Base control, EventArgs args)
        {
            if (!Value.Equals(getter()) && !isDirty)
                new ChangeValueCommand((o) => { isDirty = true; setter((T)o); }, getter(), Value).StoreCommand();
            setter(Value);
        }
    }
}