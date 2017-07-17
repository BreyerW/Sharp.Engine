using System;
using System.Collections.Generic;
using System.Reflection;
using Sharp.Editor.Attribs;
using System.Collections;
using System.Linq;
using Squid;
using Sharp.Editor.UI.Property;
using Sharp.Editor.Views;

namespace Sharp.Editor
{
    public abstract class ComponentDrawer<T>//SelectionDrawer
    {
        //private static Dictionary<Type, Base> predefinedInspectors;
        internal T getTarget;

        //internal Func<T> setTarget;
        public ComponentNode properties;

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
            Control propDrawer = null;
            var attrib = propertyInfo.GetCustomAttribute<CustomPropertyDrawerAttribute>(true);
            var attribType = attrib?.GetType();
            if (InspectorView.mappedPropertyDrawers.ContainsKey(propertyInfo.PropertyType))
                propDrawer = InspectorView.mappedPropertyDrawers[propertyInfo.PropertyType].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo }) as Control;
            else if (propertyInfo.PropertyType.GetInterfaces()
    .Any(i => i == typeof(IList)))
                propDrawer = InspectorView.mappedPropertyDrawers[typeof(IList)].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo }) as Control;
            //else
            //  propDrawer = InspectorView.mappedPropertyDrawers[typeof(object)].method.Invoke(properties, new object[] { propertyInfo.Name + ":", Target, propertyInfo }) as Control;
            if (propDrawer != null)
            {
                properties.Frame.Controls.Add(propDrawer);
            }
        }

        public void BindProperty<T1>(Func<T1> getter)
        {
        }
    }
}