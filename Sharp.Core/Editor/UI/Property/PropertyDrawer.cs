using BepuUtilities.Memory;
using FastMember;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
	public class JTokenWrapper
	{
		private JToken token;
		public JToken this[string s]
		{
			get => token[s];
			set
			{
				if (s == string.Empty)
					if (token == value) return;
				if (token[s] == value) return;
				token[s] = value;
				ApplyChanges();
			}
		}
	}
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
		private bool isDirty = false;
		//private static IEqualityComparer<T> defaultEquality=EqualityComparer<T>.Default;
		private IEnumerator<bool> savingTask;
		private JToken serializedObject;
		public MemberInfo memberInfo;
		public CustomPropertyDrawerAttribute[] attributes;
		private Component target;
		protected Label label = new Label();
		protected Type propertyType;
		//private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
		public RefAction<object, T> setter;
		public Func<object, T> getter;
		/*public abstract T Value
		{
			get;
			set;
		}*/

		/*public virtual IEqualityComparer<T> Equality
		{
			get =>defaultEquality;
		} */
		public override Component Target
		{
			set
			{
				if (target == value) return;
				//serializedObject = JToken.FromObject(getter(value), JsonSerializer.Create(MainClass.serializerSettings));
				target = value;
			}
			get => target;
		}
		public JToken SerializedObject
		{
			set
			{
				if (serializedObject == value) return;
				serializedObject = value;
				ApplyChanges();
			}
			get => serializedObject;
		}
		static PropertyDrawer()
		{
			if (scanStarted is true) return;
			SceneView.onAddedEntity += (obj) =>
			{
				if (UndoCommand.availableHistoryChanges is { } && UndoCommand.availableHistoryChanges.ContainsKey(obj.GetInstanceID())) return;
				if (saveState is null)
					saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();

				if (obj is Entity ent)
				{
					if (!saveState.ContainsKey(ent.GetInstanceID()))
						saveState.Add(ent.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
					saveState[ent.GetInstanceID()].Add("addedEntity", (null, Encoding.Unicode.GetBytes(ent.name)));
				}
				else if (obj is Component comp)
				{
					if (!saveState.ContainsKey(comp.GetInstanceID()))
						saveState.Add(comp.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
					saveState[comp.GetInstanceID()].Add("addedComponent", (null, Encoding.Unicode.GetBytes(comp.GetType().AssemblyQualifiedName)));
					saveState[comp.GetInstanceID()].Add("Parent", (null, comp.Parent.GetInstanceID().ToByteArray()));
				}
			};
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
					T val = default;
					if (UndoCommand.isUndo is false && serializedObj[Name].undo is null)
					{
						var str = Encoding.Unicode.GetString(serializedObj[Name].redo);
						val = JsonConvert.DeserializeObject<T>(str, MainClass.serializerSettings);
					}
					else
					{
						var objToBePatched = Encoding.Unicode.GetBytes(JToken.FromObject(getter(target), JsonSerializer.Create(MainClass.serializerSettings)).ToString());
						val = JsonConvert.DeserializeObject<T>(Encoding.Unicode.GetString(Fossil.Delta.Apply(objToBePatched, patch)), MainClass.serializerSettings);
					}
					var t = JToken.FromObject(val, JsonSerializer.Create(MainClass.serializerSettings));
					if (serializedObject is null)
						serializedObject = t;
					else
					{
						if (t is JValue v)
							serializedObject = v;
						else
							foreach (var prop in t)
								if (prop is JProperty p)
									serializedObject[prop.Path] = p.Value;
								else
									serializedObject[prop.Path] = prop;
					}
					setter(target, ref val);

				}
				return;
			}
			var token = JToken.FromObject(getter(target), JsonSerializer.Create(MainClass.serializerSettings));
			var get = token?.ToString();
			if (((serializedObject is { } && token is null) || (token is { } && serializedObject is null)) || !MemoryExtensions.Equals(serializedObject.ToString().AsSpan(), get.AsSpan(), StringComparison.Ordinal))
			{
				if (isDirty is false)
				{
					if (savingTask is null && target.Parent.GetComponent<Camera>() != Camera.main)
					{
						savingTask = SaveState(serializedObject?.DeepClone()).GetEnumerator();
						Console.WriteLine(" name " + Name + " target " + getter(target));
					}
					if (serializedObject is null)
						serializedObject = token;
					else
						foreach (var prop in token)
							if (prop is JProperty p)
								serializedObject[prop.Path] = p.Value;
							else
								serializedObject[prop.Path] = prop;
				}
				else
				{
					if (savingTask is null && target.Parent.GetComponent<Camera>() != Camera.main)
						savingTask = SaveState(token).GetEnumerator();
					var val = serializedObject.ToObject<T>(JsonSerializer.Create(MainClass.serializerSettings));
					setter(target, ref val);
					isDirty = false;
				}
			}
			if (!(savingTask is null) && savingTask.MoveNext() && savingTask.Current is true)
				savingTask = null;
		}
		private IEnumerable<bool> SaveState(JToken value)
		{
			while (InputHandler.isKeyboardPressed || InputHandler.isMouseDragging)
				yield return false;
			if (saveState is null)
				saveState = new Dictionary<Guid, Dictionary<string, (byte[] undo, byte[] redo)>>();
			if (!saveState.ContainsKey(target.GetInstanceID()))
				saveState.Add(target.GetInstanceID(), new Dictionary<string, (byte[] undo, byte[] redo)>());
			//MemoryMarshal.AsBytes();
			//if(SpanHelper.IsPrimitive<T>())
			//var currObjInBytes = Encoding.Default.GetBytes(JsonConvert.SerializeObject(Value, propertyType, MainClass.serializerSettings));
			var str = serializedObject is JValue val ? val.ToString(Formatting.None) : serializedObject.ToString();
			var currObjInBytes = Encoding.Unicode.GetBytes(str);
			if (value is null || saveState[target.GetInstanceID()].ContainsKey("addedComponent"))
				saveState[target.GetInstanceID()].Add(Name, (null, currObjInBytes));
			else
			{
				var prevObjInBytes = Encoding.Unicode.GetBytes(value is JValue v ? v.ToString(Formatting.None) : value.ToString());
				var delta2 = Fossil.Delta.Create(currObjInBytes, prevObjInBytes);
				var delta1 = Fossil.Delta.Create(prevObjInBytes, currObjInBytes);
				saveState[target.GetInstanceID()].Add(Name, (delta2, delta1));
			}
			//Console.WriteLine("resulting string " + JsonConvert.SerializeObject(Value, typeof(T), serializerSettings));
			yield return true;
		}
		private void ApplyChanges()
		{
			if (savingTask is { }) return;
			isDirty = true;
		}
		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (saveState is { })
				{
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