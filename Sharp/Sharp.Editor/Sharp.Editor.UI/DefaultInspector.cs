using System;
using System.Collections.Generic;
using Gwen.Control;
using Sharp.Editor.Views;
using System.Linq;
using FastMember;
using System.Linq.Expressions;
using System.Reflection;
using Sharp.Editor;

namespace Sharp.Editor.UI
{
    //[CustomInspector(typeof(object))]
    public class DefaultInspector : Inspector<object>
    {
        internal static Dictionary<Type, Action<Properties, string, object, Action<object>>> mappedPropertyDrawers = new Dictionary<Type, Action<Properties, string, object, Action<object>>>();
        internal ObjectAccessor mappedObj;

        static DefaultInspector()
        {
            var result = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
                                t.BaseType.GetGenericTypeDefinition() == typeof(PropertyDrawer<>));
            foreach (var type in result)
            {
                var obj = Activator.CreateInstance(type);
                mappedPropertyDrawers.Add(type.BaseType.GetGenericArguments()[0], (Action<Properties, string, object, Action<object>>)Delegate.CreateDelegate(typeof(Action<Properties, string, object, Action<object>>), obj, type.GetMethod("OnInitializeGUI")));


            }
        }
        public override void OnInitializeGUI()//OnSelect
        {
            mappedObj = ObjectAccessor.Create(Target);
            var props = Target.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

            foreach (var prop in props)
            {
                if (mappedPropertyDrawers.ContainsKey(prop.PropertyType))
                    mappedPropertyDrawers[prop.PropertyType]?.Invoke(properties, prop.Name, mappedObj[prop.Name], (object val) => { mappedObj[prop.Name] = val; });

            }
        }

    }
}

