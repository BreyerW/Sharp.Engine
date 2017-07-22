using System;
using Sharp.Commands;
using Sharp.Editor.Attribs;
using Squid;

namespace Sharp.Editor.UI.Property
{
    /// <summary>
    /// Base control for property entry.
    /// </summary>
    public abstract class PropertyDrawer<T> : Control//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
    {
        protected Label label = new Label();
        protected bool isDirty = false;

        public Action<T> setter;
        public Func<T> getter;

        public CustomPropertyDrawerAttribute[] attributes;

        /// <summary>
        /// Property value (todo: always string, which is ugly. do something about it).
        /// </summary>
        public abstract T Value
        {
            get;
            set;
        }

        public PropertyDrawer(string name) : base()
        {
            //Scissor = true;
            //Size = new Point(0, 20);
            label.Text = name;
            label.Size = new Point(75, Size.y);
            label.AutoEllipsis = false;
            Childs.Add(label);
            Selection.OnSelectionDirty += (sender, args) => Value = getter();
        }

        //public abstract bool IsValid(CustomPropertyDrawerAttribute[] attributes);
        private void OnValueChanged()
        {
            // if (!Value.Equals(getter()) && !Squid.UI.isDirty)
            //  new ChangeValueCommand((o) => { setter((T)o); Squid.UI.isDirty = true; }, getter(), Value).StoreCommand();
            setter(Value);
        }

        protected override void DrawBefore()
        {
            if (isDirty)
            {
                OnValueChanged();
                isDirty = false;
            }
            base.DrawBefore();
        }
    }

    public static class TypeExtensions
    {
        public static bool IsSubclassOfOpenGeneric(this Type toCheck, Type type)
        {
            if (toCheck.IsAbstract) return false;
            while (toCheck != null && toCheck != typeof(object) && toCheck != type)
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (type == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }
    }
}