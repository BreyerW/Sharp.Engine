using System;
using Sharp.Control;
using System.Reflection;
using Sharp.Editor.UI.Property;
using System.Collections;
using System.Linq;

namespace Sharp.Editor
{
    public abstract class ComponentDrawer<T>//SelectionDrawer
    {
        //private static Dictionary<Type, Base> predefinedInspectors;
        internal T getTarget;

        //internal Func<T> setTarget;
        public Properties properties;

        public T Target
        {
            get { return getTarget; }
            //set{setTarget (value);}
        }

        /*public ref T Target
        {
            get { return ref getTarget; }
            //set{setTarget (value);}
        }*/

        public ComponentDrawer()
        {
        }

        public abstract void OnInitializeGUI();

        public void BindProperty(PropertyInfo propertyInfo)
        {
            var attrib = propertyInfo.GetCustomAttribute<CustomPropertyDrawerAttribute>(true);
            var attribType = attrib?.GetType();
            if (Properties.mappedPropertyDrawers.ContainsKey((propertyInfo.PropertyType, attribType)))
                Properties.mappedPropertyDrawers[(propertyInfo.PropertyType, attribType)].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo });
            else if (propertyInfo.PropertyType.GetInterfaces()
    .Any(i => i == typeof(IList)))
                Properties.mappedPropertyDrawers[(typeof(IList), attribType)].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo });
            else
                Properties.mappedPropertyDrawers[(typeof(object), attribType)].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo });
        }

        public void BindProperty<T1>(Func<T1> getter)
        {
        }
    }
}