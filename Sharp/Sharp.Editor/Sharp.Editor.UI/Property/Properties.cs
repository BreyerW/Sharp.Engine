using System;
using System.Reflection;
using Gwen.Control;
using Gwen;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using Sharp.Editor.UI.Property;

namespace Sharp.Control
{
    /// <summary>
    /// Properties table.
    /// </summary>
    public class Properties : Base
    {
        //private readonly SplitterBar m_SplitterBar;

        /// <summary>
        /// Returns the width of the first column (property names).
        /// </summary>
        //public int SplitWidth { get { return m_SplitterBar.X; } } // todo: rename?

        /// <summary>
        /// Invoked when a property value has been changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> ValueChanged;

        internal static Dictionary<(Type primitiveType, Type attribType), (Type type, MethodInfo method)> mappedPropertyDrawers = new Dictionary<(Type primitiveType, Type attribType), (Type type, MethodInfo method)>();

        static Properties()
        {
            //var primitiveResult = Assembly.GetExecutingAssembly()
            //   .GetTypes()
            // .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
            //      t.BaseType.GetGenericTypeDefinition() == typeof(Gwen.Control.Property.PropertyDrawer<>));//current solution does not support color case

            var result = Assembly.GetExecutingAssembly().GetTypes()
             .Where(t => t.IsSubclassOfOpenGeneric(typeof(Gwen.Control.Property.PropertyDrawer<>)));

            var method = typeof(Properties).GetMethod("Add");
            Type[] genericArgs;
            MethodInfo genericMethod;

            foreach (var type in result)
            {
                genericArgs = type.BaseType.GetGenericArguments();
                genericMethod = method.MakeGenericMethod(new[] { genericArgs[0] });
                mappedPropertyDrawers.Add((genericArgs[0], genericArgs.Length > 1 ? genericArgs[1] : null), (type, genericMethod));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Properties"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public Properties(Base parent)
                : base(parent)
        {
            //m_SplitterBar = new SplitterBar(this);
            // m_SplitterBar.SetPosition(80, 0);
            // m_SplitterBar.Cursor = Cursors.SizeWE;
            //m_SplitterBar.Dragged += OnSplitterMoved;
            // m_SplitterBar.ShouldDrawBackground = false;
        }

        /// <summary>
        /// Function invoked after layout.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void PostLayout(Gwen.Skin.Base skin)
        {
            // m_SplitterBar.Height = 0;

            if (SizeToChildren(false, true))
            {
                InvalidateParent();
            }

            // m_SplitterBar.SetSize(3, Height);
        }

        /// <summary>
        /// Handles the splitter moved event.
        /// </summary>
        /// <param name="control">Event source.</param>
        protected virtual void OnSplitterMoved(Base control, EventArgs args)
        {
            InvalidateChildren();
        }

        /// <summary>
        /// Adds a new property row.
        /// </summary>
        /// <param name="label">Property name.</param>
        /// <param name="prop">Property control.</param>
        /// <param name="value">Initial value.</param>
        /// <returns>Newly created row.</returns>
        public PropertyRow<T> Add<T>(string label, object instance, PropertyInfo propertyInfo)
        {
            if (Get<T>(propertyInfo.Name + ":") is PropertyRow<T> tmpRow) return tmpRow;

            Gwen.Control.Property.PropertyDrawer<T> prop;
            var attrib = propertyInfo.GetCustomAttribute<CustomPropertyDrawerAttribute>(true);//customattributes when supporting priority/overriding
                                                                                              // if (attrib is null)
            {
                if (mappedPropertyDrawers.ContainsKey((typeof(T), attrib?.GetType())))
                    prop = Activator.CreateInstance(mappedPropertyDrawers[(typeof(T), attrib?.GetType())].type, this) as Gwen.Control.Property.PropertyDrawer<T>;
                else if (propertyInfo.PropertyType.GetInterfaces()
    .Any(i => i == typeof(IList)))//isassignablefrom?
                {
                    Console.WriteLine("array");
                    prop = new ArrayDrawer(this) as Gwen.Control.Property.PropertyDrawer<T>;
                }
                else
                    prop = Activator.CreateInstance(mappedPropertyDrawers[(typeof(object), attrib?.GetType())].type, this) as Gwen.Control.Property.PropertyDrawer<T>;
            }

            PropertyRow<T> row = new PropertyRow<T>(this, prop);
            row.Dock = Pos.Top;
            row.Label = label;
            row.Name = label;
            row.ValueChanged += OnRowValueChanged;
            row.getter = DelegateGenerator.GenerateGetter<T>(instance, propertyInfo);
            row.setter = DelegateGenerator.GenerateSetter<T>(instance, propertyInfo);
            prop.SetValue(row.getter(), true);

            // m_SplitterBar.BringToFront();
            return row;
        }

        public PropertyRow<T> Get<T>(string label)
        {
            return FindChildByName(label) as PropertyRow<T>;
        }

        private void OnRowValueChanged(Base control, EventArgs args)
        {
            if (ValueChanged != null)
                ValueChanged.Invoke(control, EventArgs.Empty);
        }

        /// <summary>
        /// Deletes all rows.
        /// </summary>
        public void DeleteAll()
        {
            m_InnerPanel.DeleteAllChildren();
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