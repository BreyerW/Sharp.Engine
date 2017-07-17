using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public virtual bool IsEditing { get { return false; } }

        public PropertyDrawer(string name) : base()
        {
            //Scissor = true;
            //Size = new Point(0, 20);
            label.Text = name;
            label.Size = new Point(75, Size.y);
            label.AutoEllipsis = false;
            Childs.Add(label);
        }

        //public abstract bool IsValid(CustomPropertyDrawerAttribute[] attributes);
        /*   protected void OnValueChanged(Control.Base control, EventArgs args)
           {
               if (!Value.Equals(getter()) && !isDirty)
                   new ChangeValueCommand((o) => { isDirty = true; setter((T)o); }, getter(), Value).StoreCommand();
               setter(Value);
           }*/
        /*
         if (isDirty)
                Value = m_Property.getter();*/
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