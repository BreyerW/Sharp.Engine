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

		protected Label label = new Label();

		public PropertyDrawer(string name) : base()
		{
			//Scissor = true;
			//Size = new Point(0, 20);

			Name = name;
			label.Text = name;
			label.Size = new Point(75, Size.y);
			label.AutoEllipsis = false;
			Childs.Add(label);
			LateUpdate += PropertyDrawer_Update;
		}

		protected abstract void PropertyDrawer_Update(Control sender);

		internal abstract void GenerateSetterGetter(object instance);
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer//if u want support multiple types with same drawer use object, object have least priority compared to same attrib but specialized drawer
	{
		//private T prevUIValue;
		private T prevValue;
		private bool isPropertyDirty = true;

		private static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
		{
			ContractResolver = new DefaultContractResolver() { IgnoreSerializableAttribute = false },
			Converters = new List<JsonConverter>() { new DelegateConverter(), new ListReferenceConverter() },
			ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
			PreserveReferencesHandling = PreserveReferencesHandling.All,
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			TypeNameHandling = TypeNameHandling.All,
			ObjectCreationHandling = ObjectCreationHandling.Auto,
			ReferenceResolverProvider = () => new ThreadsafeReferenceResolver(),
			//NullValueHandling = NullValueHandling.Ignore
		};

		private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
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

		internal sealed override void GenerateSetterGetter(object instance)
		{
			if (typeof(T) == memberInfo.GetUnderlyingType())
			{
				getter = DelegateGenerator.GenerateGetter<T>(memberInfo);
				setter = DelegateGenerator.GenerateSetter<T>(memberInfo);
			}
			// else { //happens usually when propertyDrawer is default
			//    DelegateGenerator.GenerateGetter
			//}
			Value = getter(instance);
			prevValue = Value;
			//Console.WriteLine("test:" + ((FieldInfo)memberInfo).FieldType);
		}

		protected override void PropertyDrawer_Update(Control sender)
		{
			var refComp = (Parent.Parent as ComponentNode).referencedComponent;
			//if (!(Desktop.FocusedControl is Views.SceneView) && !Value.Equals(getter((Parent.Parent as ComponentNode).referencedComponent)))//&& !(InputHandler.isKeyboardPressed | InputHandler.isMouseDragging)

			//if prev value != object value and prev value == ui value then its obvius ui is outdated. In reverse case its object thats outdated
			if (!prevValue.Equals(getter(refComp)))
			{
				Console.WriteLine("ui outdated");
				Value = getter(refComp);
				prevValue = getter(refComp);
				if ((refComp as Component).Parent.GetComponent<Camera>() != Camera.main)
					isPropertyDirty = true;
			}
			else if (!prevValue.Equals(Value))
			{
				Console.WriteLine("object outdated");
				//var refC = TypedReference.MakeTypedReference(refComp, new FieldInfo[] { (FieldInfo)memberInfo });
				//((FieldInfo)memberInfo).SetValueDirect(refC,Value);
				setter(refComp, Value);
				prevValue = getter(refComp);
				if ((refComp as Component).Parent.GetComponent<Camera>() != Camera.main)
					isPropertyDirty = true;
			}
			if (InspectorView.availableUndoRedo is { }
			&& InspectorView.availableUndoRedo.TryGetValue((refComp.GetInstanceID(), Name), out var serializedObj))
			{
				if (typeof(T) is IEngineObject)
					prevValue = new Guid(serializedObj).GetInstanceObject<T>();
				else if (typeof(T) is IAsset asset)
					prevValue = (T)Pipeline.allPipelines[typeof(T)].Import(asset.FullPath);
				else
					prevValue = JsonConvert.DeserializeObject<T>(serializedObj, serializerSettings);
				Value = prevValue;
				setter(refComp, prevValue);
				//imagine a case where you rotate something in SceneView by hand and at the same time hit Undo/Redo shortcut then immediately stop rotating - in this case we dont want saving identical values.
				//It is also useful when restoring objects from Undo/Redo stack since isPropertyDirty = true by default upon creation.
				isPropertyDirty = false;
				return;
			}
			if (!InputHandler.isKeyboardPressed && !InputHandler.isMouseDragging && isPropertyDirty)
			{
				if (InspectorView.saveState is null)
					InspectorView.saveState = new Dictionary<(Guid, string), string>();
				if (typeof(T) is IEngineObject)
					InspectorView.saveState.Add((refComp.GetInstanceID(), Name), Value.GetInstanceID().ToString());
				else if (typeof(T) is IAsset asset)
					InspectorView.saveState.Add((refComp.GetInstanceID(), Name), asset.FullPath);
				else
				{
					InspectorView.saveState.Add((refComp.GetInstanceID(), Name), JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));
					//Console.WriteLine("resulting string " + JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));
				}
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