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
        public bool propertyIsDirty = false;

        private ChangeValueCommand command = null;

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
            Selection.OnSelectionDirty += (sender, args) =>
            {
                Value = getter();
            };
            Window.onBeforeNextFrame += () =>
            {
                if (propertyIsDirty)
                {
                    CreateCommand(command is null);

                    propertyIsDirty = false;
                }
                if (!PropertyDrawer.StopCommandCommits && command != null)
                {
                    object obj = getter();
                    if (obj is float || obj is double || obj is decimal)
                        obj = Math.Round((decimal)obj, Application.roundingPrecision);
                    else if (obj is OpenTK.Vector3 vec)
                        obj = new OpenTK.Vector3((float)Math.Round(vec.X, Application.roundingPrecision), (float)Math.Round(vec.Y, Application.roundingPrecision), (float)Math.Round(vec.Z, Application.roundingPrecision));
                    if (!obj.Equals(command.newValue))//take rounding into account
                    {
                        command.newValue = Value;
                        command.StoreCommand();
                    }
                    setter(Value);
                    command = null;
                }
            };
            PropertyDrawer.onCommandBehaviourChanged += CreateCommand;
        }

        private void CreateCommand(bool create)
        {
            if (create)
            {
                command = new ChangeValueCommand((o) => { setter((T)o); Value = getter(); propertyIsDirty = false; }, getter(), Value);
                Console.WriteLine("save " + Value + " " + getter());
            }
        }

        protected override void DrawBefore()
        {
            base.DrawBefore();
        }

        //public abstract bool IsValid(CustomPropertyDrawerAttribute[] attributes);
    }

    public static class PropertyDrawer
    {
        private static bool stopCommandCommits = false;
        public static Action<bool> onCommandBehaviourChanged;

        public static bool StopCommandCommits
        {
            set
            {
                if (value != stopCommandCommits)
                    onCommandBehaviourChanged?.Invoke(value);
                stopCommandCommits = value;
            }
            internal get => stopCommandCommits;
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