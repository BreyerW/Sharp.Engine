using FastMember;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sharp.Editor.Attribs;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using Squid;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		public MemberInfo memberInfo;
		public CustomPropertyDrawerAttribute[] attributes;
		public Component target;
		protected Label label = new Label();
		protected Type propertyType;
		public PropertyDrawer(MemberInfo memInfo) : base()
		{
			//Scissor = true;
			//Size = new Point(0, 20);
			memberInfo = memInfo;
			Name = memberInfo.Name;
			propertyType = memberInfo.GetUnderlyingType();
			propertyType = propertyType.IsByRef ? propertyType.GetElementType() : propertyType;
			label.Text = memberInfo.Name + ": ";
			label.Size = new Point(75, Size.y);
			label.AutoEllipsis = false;
			Childs.Add(label);
			LateUpdate += PropertyDrawer_Update;
		}

		protected abstract void PropertyDrawer_Update(Control sender);
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
	{
		private T prevValue;
		private bool isPropertyDirty = true;

		//private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
		public RefAction<object, T> setter;
		public Func<object, T> getter;
		public abstract T Value
		{
			get;
			set;
		}
		public PropertyDrawer(MemberInfo memInfo) : base(memInfo)
		{
			(getter, setter) = DelegateGenerator.GetAccessors<T>(memberInfo);
		}

		protected override void PropertyDrawer_Update(Control sender)
		{
			//if (!(refComp as Component).active) return;
			//if (!(Desktop.FocusedControl is Views.SceneView) && !Value.Equals(getter((Parent.Parent as ComponentNode).referencedComponent)))//&& !(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging)

			//if prev value != object value and prev value == ui value then its obvius ui is outdated. In reverse case its object thats outdated
			if (InspectorView.availableUndoRedo is { }
			&& InspectorView.availableUndoRedo.TryGetValue(target.GetInstanceID(), out var serializedObj))
			{
				if (serializedObj.ContainsKey(Name))
				{
					prevValue = (T)JsonConvert.DeserializeObject(serializedObj[Name], propertyType, MainClass.serializerSettings);
					Value = prevValue;
					if (Name is "curves")
						Console.WriteLine(prevValue);
					setter(target, ref prevValue);
				}
				//imagine a case where you rotate something in SceneView by hand and at the same time hit Undo/Redo shortcut then immediately stop rotating - in this case we dont want saving identical values.
				//It is also useful when restoring objects from Undo/Redo stack since isPropertyDirty = true by default upon creation.
				isPropertyDirty = false;
				return;
			}
			var get = getter(target);
			if (!get.Equals(prevValue))
			{
				Console.WriteLine("ui outdated");
				prevValue = get;
				Value = prevValue;
				if (target.Parent.GetComponent<Camera>() != Camera.main)
					isPropertyDirty = true;
			}
			else if (!prevValue.Equals(Value))
			{
				Console.WriteLine("object outdated ");
				//var refC = TypedReference.MakeTypedReference(refComp, new FieldInfo[] { (FieldInfo)memberInfo });
				//((FieldInfo)memberInfo).SetValueDirect(refC,Value);
				prevValue = Value;// getter(refComp);
				setter(target, ref prevValue);

				if (target.Parent.GetComponent<Camera>() != Camera.main)
					isPropertyDirty = true;
			}

			if (!InputHandler.isKeyboardPressed && !InputHandler.isMouseDragging && isPropertyDirty)
			{
				var type = memberInfo.GetUnderlyingType();
				type = type.IsByRef ? type.GetElementType() : type;
				if (InspectorView.saveState is null)
					InspectorView.saveState = new Dictionary<Guid, Dictionary<string, string>>();
				if (!InspectorView.saveState.ContainsKey(target.GetInstanceID()))
					InspectorView.saveState.Add(target.GetInstanceID(), new Dictionary<string, string>());
				InspectorView.saveState[target.GetInstanceID()].Add(Name, JsonConvert.SerializeObject(Value, type, MainClass.serializerSettings));
				//Console.WriteLine("resulting string " + JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));

				isPropertyDirty = false;
			}
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