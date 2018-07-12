using Sharp.Editor.Attribs;
using Squid;
using System;
using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		public MemberInfo memberInfo;
		public CustomPropertyDrawerAttribute[] attributes;

		protected Label label = new Label();

		//protected bool propertyIsFocused = false;

		public PropertyDrawer(string name) : base()
		{
			//Scissor = true;
			//Size = new Point(0, 20);
			label.Text = name;
			label.Size = new Point(75, Size.y);
			label.AutoEllipsis = false;
			Childs.Add(label);
			LateUpdate += PropertyDrawer_Update;
			//Childs.BeforeItemAdded += Childs_BeforeItemAdded;
		}

		/* private void AddFocusEvents(Control control)
         {
             IControlContainer container = control as IControlContainer;
             if (container is null)
                 control.Childs.BeforeItemAdded += Childs_BeforeItemAdded;
             else
                 container.Controls.BeforeItemAdded += Childs_BeforeItemAdded;
             control.GotFocus += Item_GotFocus;
             control.LostFocus += Item_LostFocus;
             if (control.Childs.Count > 0)
                 foreach (var child in control.Childs)
                     AddFocusEvents(child);
             else if (container?.Controls.Count > 0)
                 foreach (var child in container.Controls)
                     AddFocusEvents(child);
         }

         private void Childs_BeforeItemAdded(object sender, ListEventArgs<Control> e)
         {
             //AddFocusEvents(e.Item);
         }

         private void Item_LostFocus(Control sender)
         {
             propertyIsFocused = false;
         }

         protected void Item_GotFocus(Control sender)
         {
             propertyIsFocused = true;
         }*/

		protected abstract void PropertyDrawer_Update(Control sender);

		internal abstract void GenerateSetterGetter();
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
	{
		private T prevUIValue;
		private T prevObjValue;

		public Action<object, T> setter;
		public Func<object, T> getter;

		public abstract T Value
		{
			get;
			set;
		}

		public PropertyDrawer(string name) : base(name)
		{
		}

		internal sealed override void GenerateSetterGetter()
		{
			if (typeof(T) == memberInfo.GetUnderlyingType())
			{
				getter = DelegateGenerator.GenerateGetter<T>(memberInfo);
				setter = DelegateGenerator.GenerateSetter<T>(memberInfo);
			}
			// else { //happens usually when propertyDrawer is default
			//    DelegateGenerator.GenerateGetter
			//}
			var refComp = (Parent.Parent as ComponentNode).referencedComponent;
			Value = getter(refComp);
			prevUIValue = Value;
			prevObjValue = getter(refComp);
		}

		protected override void PropertyDrawer_Update(Control sender)
		{
			var refComp = (Parent.Parent as ComponentNode).referencedComponent;
			//if (!(Desktop.FocusedControl is Views.SceneView) && !Value.Equals(getter((Parent.Parent as ComponentNode).referencedComponent)))//&& !(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging)
			if (!prevUIValue.Equals(Value))
			{
				setter(refComp, Value);
				prevUIValue = Value;
			}
			else if (!prevObjValue.Equals(getter(refComp))) //if (!Value.Equals(getter((Parent.Parent as ComponentNode).referencedComponent)))
			{
				Value = getter(refComp);
				prevObjValue = getter(refComp);
			}
		}

		protected override void DrawBefore()
		{
			base.DrawBefore();
		}

		//public abstract bool IsValid(CustomPropertyDrawerAttribute[] attributes);
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