using FastMember;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Sharp.Editor.Attribs;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		protected internal static bool scanStarted = false;
		protected internal static Dictionary<Guid, Dictionary<string, string>> saveState = new Dictionary<Guid, Dictionary<string, string>>();

		public abstract Component Target
		{
			set;
			get;
		}
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
	{
		private T prevValue;
		private bool isPropertyDirty = true;
		public MemberInfo memberInfo;
		public CustomPropertyDrawerAttribute[] attributes;
		private Component target;
		protected Label label = new Label();
		protected Type propertyType;
		//private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
		public RefAction<object, T> setter;
		public Func<object, T> getter;
		public abstract T Value
		{
			get;
			set;
		}
		public override Component Target
		{
			set
			{
				if (target == value) return;
				target = value;
				if (UndoCommand.availableUndoRedo is null || !UndoCommand.availableUndoRedo.ContainsKey(target.GetInstanceID()))
				{
					if (saveState is null)
						saveState = new Dictionary<Guid, Dictionary<string, string>>();
					if (!saveState.ContainsKey(target.GetInstanceID()))
						saveState.Add(target.GetInstanceID(), new Dictionary<string, string>());
					else if (saveState[target.GetInstanceID()].ContainsKey("addedComponent")) return;
					saveState[target.GetInstanceID()].Add("addedComponent", target.GetType().AssemblyQualifiedName);
					saveState[target.GetInstanceID()].Add("Parent", target.Parent.GetInstanceID().ToString());

					if (!saveState.ContainsKey(target.Parent.GetInstanceID()))
						saveState.Add(target.Parent.GetInstanceID(), new Dictionary<string, string>());
					else if (saveState[target.Parent.GetInstanceID()].ContainsKey("addedEntity")) return;
					saveState[target.Parent.GetInstanceID()].Add("addedEntity", target.Parent.name);
				}
			}
			get => target;
		}
		static PropertyDrawer()
		{
			if (scanStarted is false)
			{
				Coroutine.Start(SaveChangesBeforeNextFrame());
				scanStarted = true;
			}
		}
		public PropertyDrawer(MemberInfo memInfo)
		{
			memberInfo = memInfo;
			(getter, setter) = DelegateGenerator.GetAccessors<T>(memberInfo);
			//Scissor = true;
			//Size = new Point(0, 20);
			Name = memberInfo.Name;
			propertyType = memberInfo.GetUnderlyingType();
			propertyType = propertyType.IsByRef ? propertyType.GetElementType() : propertyType;
			label.Text = memberInfo.Name + ": ";
			label.Size = new Point(75, Size.y);
			label.AutoEllipsis = false;
			Childs.Add(label);
			LateUpdate += PropertyDrawer_Update;
		}

		protected void PropertyDrawer_Update(Control sender)
		{
			//if (!(refComp as Component).active) return;
			//if (!(Desktop.FocusedControl is Views.SceneView) && !Value.Equals(getter((Parent.Parent as ComponentNode).referencedComponent)))//&& !(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging)

			//if prev value != object value and prev value == ui value then its obvius ui is outdated. In reverse case its object thats outdated
			if (UndoCommand.availableUndoRedo is { }
			&& UndoCommand.availableUndoRedo.TryGetValue(target.GetInstanceID(), out var serializedObj))
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
				if (saveState is null)
					saveState = new Dictionary<Guid, Dictionary<string, string>>();
				if (!saveState.ContainsKey(target.GetInstanceID()))
					saveState.Add(target.GetInstanceID(), new Dictionary<string, string>());
				saveState[target.GetInstanceID()].Add(Name, JsonConvert.SerializeObject(Value, type, MainClass.serializerSettings));
				//Console.WriteLine("resulting string " + JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));

				isPropertyDirty = false;
			}
		}
		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (saveState is { })
				{
					/*if (saveState is null)
						saveState = new Dictionary<Guid, Dictionary<string, string>>();
					if (!saveState.ContainsKey(currentlyDrawedObject))
						saveState.Add(currentlyDrawedObject, new Dictionary<string, string>());
					saveState[currentlyDrawedObject].Add("selected", "");*/
					SaveChanges(saveState);
					saveState = null;
				}
				UndoCommand.availableUndoRedo = null;
			}
		}
		private static void SaveChanges(Dictionary<Guid, Dictionary<string, string>> toBeSaved)
		{
			if (UndoCommand.currentHistory is { } && UndoCommand.currentHistory.Next is { }) //TODO: this is bugged state on split is doubled for some reason
			{
				UndoCommand.currentHistory.RemoveAllAfter();
				Console.WriteLine("clear trailing history");
			}
			var finalSave = new Dictionary<Guid, Dictionary<string, string>>();
			foreach (var (index, val) in toBeSaved)
			{
				finalSave.Add(index, val);
			}
			UndoCommand.snapshots.AddLast(new History() { propertyMapping = finalSave });
			UndoCommand.currentHistory = UndoCommand.snapshots.Last;
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