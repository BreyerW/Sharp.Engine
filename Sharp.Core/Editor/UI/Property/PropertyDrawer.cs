using BepuUtilities.Memory;
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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		protected internal static bool scanStarted = false;
		protected internal static Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>> saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();

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
		private IEnumerator<bool> savingTask;
		private T prevValue;
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
				if (UndoCommand.availableHistoryChanges is null || !UndoCommand.availableHistoryChanges.ContainsKey(target.GetInstanceID()))
				{
					if (saveState is null)
						saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();
					if (!saveState.ContainsKey(target.GetInstanceID()))
						saveState.Add(target.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
					else if (saveState[target.GetInstanceID()].ContainsKey("addedComponent")) return;
					saveState[target.GetInstanceID()].Add("addedComponent", (null, Encoding.Default.GetBytes(target.GetType().AssemblyQualifiedName)));
					saveState[target.GetInstanceID()].Add("Parent", (null, target.Parent.GetInstanceID().ToByteArray()));

					if (!saveState.ContainsKey(target.Parent.GetInstanceID()))
						saveState.Add(target.Parent.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
					else if (saveState[target.Parent.GetInstanceID()].ContainsKey("addedEntity")) return;
					saveState[target.Parent.GetInstanceID()].Add("addedEntity", (null, Encoding.Default.GetBytes(target.Parent.name)));
				}
			}
			get => target;
		}
		static PropertyDrawer()
		{
			if (scanStarted is true) return;
			Selection.OnSelectionChange += (old, s) =>
			{
				if (s is IEngineObject o)
				{
					if (UndoCommand.availableHistoryChanges is { } && UndoCommand.availableHistoryChanges[o.GetInstanceID()].ContainsKey("selected")) return;
					if (saveState is null)
						saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();
					if (!saveState.ContainsKey(o.GetInstanceID()))
						saveState.Add(o.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());

					saveState[o.GetInstanceID()].Add("selected", (old is null ? null : old.GetInstanceID().ToByteArray(), s is null ? null : s.GetInstanceID().ToByteArray()));
				}
			};
			Coroutine.Start(SaveChangesBeforeNextFrame());
			scanStarted = true;
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
			if (UndoCommand.availableHistoryChanges is { }
			&& UndoCommand.availableHistoryChanges.TryGetValue(target.GetInstanceID(), out var serializedObj))
			{
				if (serializedObj.ContainsKey(Name))
				{
					var patch = UndoCommand.isUndo ? serializedObj[Name].undo : serializedObj[Name].redo;
					if (UndoCommand.isUndo is false && serializedObj[Name].undo is null)
						prevValue = (T)JsonConvert.DeserializeObject(Encoding.Default.GetString(serializedObj[Name].redo), propertyType, MainClass.serializerSettings);
					else
					{
						var objToBePatched = Encoding.Default.GetBytes(JsonConvert.SerializeObject(getter(target), propertyType, MainClass.serializerSettings));
						prevValue = (T)JsonConvert.DeserializeObject(Encoding.Default.GetString(Fossil.Delta.Apply(objToBePatched, patch)), propertyType, MainClass.serializerSettings);
					}
					Value = prevValue;
					//if (Name is "curves")
					//	Console.WriteLine(prevValue);
					setter(target, ref prevValue);
				}
				return;
			}
			var get = getter(target);
			if (!get.Equals(prevValue))
			{
				Console.WriteLine("ui outdated");
				if (savingTask is null && target.Parent.GetComponent<Camera>() != Camera.main)
					savingTask = SaveState(false, Value).GetEnumerator();
				prevValue = get;
				Value = prevValue;
			}
			else if (!prevValue.Equals(Value))
			{
				Console.WriteLine("object outdated ");
				//var refC = TypedReference.MakeTypedReference(refComp, new FieldInfo[] { (FieldInfo)memberInfo });
				//((FieldInfo)memberInfo).SetValueDirect(refC,Value);
				if (savingTask is null && target.Parent.GetComponent<Camera>() != Camera.main)
					savingTask = SaveState(false, get).GetEnumerator();
				prevValue = Value;// getter(refComp);
				setter(target, ref prevValue);
			}
			if (!(savingTask is null) && savingTask.MoveNext() && savingTask.Current is true)
				savingTask = null;
		}
		private IEnumerable<bool> SaveState(bool isObjectDirty, T value)
		{
			while (InputHandler.isKeyboardPressed || InputHandler.isMouseDragging)
				yield return false;
			if (saveState is null)
				saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();
			if (!saveState.ContainsKey(target.GetInstanceID()))
				saveState.Add(target.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
			//MemoryMarshal.AsBytes();
			//if(SpanHelper.IsPrimitive<T>())
			var currObjInBytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(Value, propertyType, MainClass.serializerSettings));
			if (value is null || saveState[target.GetInstanceID()].ContainsKey("addedComponent"))
				saveState[target.GetInstanceID()].Add(Name, (null, currObjInBytes));
			else
			{
				var prevObjInBytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(value, propertyType, MainClass.serializerSettings));
				var delta2 = Fossil.Delta.Create(currObjInBytes, prevObjInBytes);
				var delta1 = Fossil.Delta.Create(prevObjInBytes, currObjInBytes);
				saveState[target.GetInstanceID()].Add(Name, isObjectDirty ? (delta1, delta2) : (delta2, delta1));
			}
			//Console.WriteLine("resulting string " + JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));
			yield return true;
		}

		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (saveState is { })
				{
					/*if (Selection.Asset is { })
					{
						if (!saveState.ContainsKey(Selection.Asset.GetInstanceID()))
							saveState.Add(Selection.Asset.GetInstanceID(), new Dictionary<string, string>());
						saveState[Selection.Asset.GetInstanceID()].Add("selected", "");
					}*/
					SaveChanges(saveState);
					saveState = null;
				}
				UndoCommand.availableHistoryChanges = null;
			}
		}
		private static void SaveChanges(Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>> toBeSaved)
		{
			if (UndoCommand.currentHistory is { } && UndoCommand.currentHistory.Next is { }) //TODO: this is bugged state on split is doubled for some reason
			{
				UndoCommand.currentHistory.RemoveAllAfter();
				Console.WriteLine("clear trailing history");
			}
			var finalSave = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();
			foreach (var (index, val) in toBeSaved)
			{
				/*var o = index.GetInstanceObject();
				if (o is Component c)
					if (UndoCommand.currentHistory is { } && UndoCommand.currentHistory.Value.propertyMapping.TryGetValue(c.Parent.GetInstanceID(), out var changes) && changes.ContainsKey("selected"))
						if (UndoCommand.currentHistory.Previous.Value.propertyMapping.TryGetValue(index, out var prevChanges))
						{
							UndoCommand.currentHistory.Value.propertyMapping.Add(c.GetInstanceID(), new Dictionary<string, string>());
							foreach (var i in val)
							{
								if (UndoCommand.currentHistory.Value.propertyMapping[c.GetInstanceID()].ContainsKey(i.Key)) continue;
								UndoCommand.currentHistory.Value.propertyMapping[c.GetInstanceID()].Add(i.Key, prevChanges[i.Key]);
							}
						}*/
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