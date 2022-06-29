using Fossil;
using Microsoft.Collections.Extensions;
using Sharp.Core;
using Sharp.Engine.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Sharp.Editor
{
	public struct History
	{
		//public byte[] downgrade;
		//public byte[] upgrade;
		public Dictionary<Guid, (string label, byte[] undo, byte[] redo)> propertyMapping;
		//public Guid? selectedObject;
	}

	public class UndoCommand : IMenuCommand
	{
		private static Dictionary<Guid, (string label, byte[] undo, byte[] redo)> saveState = new Dictionary<Guid, (string label, byte[] undo, byte[] redo)>();
		internal static DictionarySlim<IEngineObject, byte[]> prevStates = new();
		internal static LinkedList<History> snapshots = new LinkedList<History>();

		internal static LinkedListNode<History> currentHistory;
		internal static bool isUndo = false;
		public string menuPath => "Undo";
		internal static bool historyMoved = false;
		public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

		public string Indentifier { get => menuPath; }

		//public static Stack<ICommand> done = new Stack<ICommand>();
		static UndoCommand()
		{
			Selection.OnSelectionChange += (old, s) =>
			{
				if (s is IEngineObject o && historyMoved is false)
				{
					if (saveState is null)
						saveState = new Dictionary<Guid, (string label, byte[] undo, byte[] redo)>();

					saveState[o.GetInstanceID()] = ("selected", old?.GetInstanceID().ToByteArray(), s?.GetInstanceID().ToByteArray());
				}
			};
			Coroutine.Start(SaveChangesBeforeNextFrame());
		}
		public void Execute(bool reverse = true)
		{
			if (UndoCommand.currentHistory.Previous is null)
				return;
			historyMoved = true;
			isUndo = true;
			foreach (var (index, (label, undo, _)) in UndoCommand.currentHistory.Value.propertyMapping)
			{
				if (label is "addedEntity")
				{
					index.GetInstanceObject<Entity>()?.Dispose();
				}
				else if (label is "removedEntity")
				{
					//var entity = RuntimeHelpers.GetUninitializedObject(typeof(Entity));//pseudodeserialization thats why we use this
				}
				else if (label is "addedComponent")
				{
					//InspectorView.saveState.Remove(index);
					index.GetInstanceObject<Component>()?.Dispose();
					//componentsToBeAdded.Add(index.Item1, val);
				}
				else if (label is "removedComponent")
				{
					//componentsToBeAdded.Add(index.Item1, val);

				}
				else if (label is "addedSystem")
				{
					throw new NotSupportedException("Systems are not implemented yet");

				}
				else if (label is "changed")
				{
					var obj = index.GetInstanceObject<IEngineObject>();
					ref var patched = ref prevStates.GetOrAddValueRef(obj);
					patched = Delta.Apply(patched, undo);
					PluginManager.serializer.Deserialize(patched, obj.GetType());
					if (obj is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
						startable.Start();

				}
				if (label is "selected")
				{
					var obj = undo is null ? null : new Guid(undo).GetInstanceObject();
					Selection.Asset = obj;
				}
			}

			//foreach (var (index, list) in UndoCommand.currentHistory.Value.propertyMapping)
			{


				//if (obj is Camera cam && cam == Camera.main) continue;
			}
			currentHistory = UndoCommand.currentHistory.Previous;
			Squid.UI.isDirty = true;
		}
		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (Root.addedEntities.Count > 0 && historyMoved is false)
					foreach (var added in Root.addedEntities)
					{
						if (saveState is null)
							saveState = new Dictionary<Guid, (string, byte[] undo, byte[] redo)>();

						if (added is Entity ent)
						{
							prevStates.GetOrAddValueRef(ent) = PluginManager.serializer.Serialize(ent, ent.GetType());
							saveState[ent.GetInstanceID()] = ("addedEntity", null, prevStates.GetOrAddValueRef(ent));
						}
						else if (added is Component comp)
						{
							if (prevStates.TryGetValue(comp, out _)) throw new InvalidOperationException("unexpected add to already existing key");

							prevStates.GetOrAddValueRef(comp) = PluginManager.serializer.Serialize(comp, comp.GetType());

							saveState[comp.GetInstanceID()] = ("addedComponent", null, prevStates.GetOrAddValueRef(comp));
						}
					};
				if (Root.removedEntities.Count > 0)
					foreach (var removed in Root.removedEntities)
						if (removed is IEngineObject comp)
							prevStates.Remove(comp);
				if (InputHandler.isKeyboardPressed || InputHandler.isMouseDragging)
					yield return new WaitForEndOfFrame();
				Console.WriteLine(InputHandler.isKeyboardPressed || InputHandler.isMouseDragging);
				if (historyMoved is false && !InputHandler.isKeyboardPressed && !InputHandler.isMouseDragging)//TODO: change to on mouse up/keyboard up?
				{
					//if(Editor.isObjectsDirty is DirtyState.All)
					foreach (var (comp, state) in prevStates)
					{
						var token = PluginManager.serializer.Serialize(comp, comp.GetType());

						if (!state.AsSpan().SequenceEqual(token.AsSpan()))
						{
							if (comp is Component c && c.Parent.GetComponent<Camera>() == Camera.main) continue;

							Console.WriteLine(" name " + /*Name + */" target " + comp);
							if (saveState is null)
								saveState = new Dictionary<Guid, (string, byte[] undo, byte[] redo)>();
							//MemoryMarshal.AsBytes();
							//if(SpanHelper.IsPrimitive<T>())
							var str = token;
							var currObjInBytes = str;
							if (state is null /*|| saveState[comp.GetInstanceID()].ContainsKey("addedComponent")*/)
								saveState[comp.GetInstanceID()] = ("changed", null, currObjInBytes);
							else
							{
								var prevObjInBytes = state;
								var delta2 = Delta.Create(currObjInBytes, prevObjInBytes);
								var delta1 = Delta.Create(prevObjInBytes, currObjInBytes);
								saveState[comp.GetInstanceID()] = ("changed", delta2, delta1);
							}
							prevStates.GetOrAddValueRef(comp) = token;
						}
					}
					/*else if(Editor.isObjectsDirty is DirtyState.OneOrMore)
							{
								while(Editor.dirtyObjects.TryDequeue(out var dirtyObj))
								{
									var token = PluginManager.serializer.Serialize(dirtyObj, dirtyObj.GetType());

									if (prevStates.TryGetValue(dirtyObj, out var state) && state.AsSpan().SequenceEqual(token.AsSpan()) is false)
									{
										if (dirtyObj is Component c && c.Parent.GetComponent<Camera>() == Camera.main) continue;

										Console.WriteLine(" name " + /*Name + *" target " + dirtyObj);
										if (saveState is null)
											saveState = new Dictionary<Guid, (string, byte[] undo, byte[] redo)>();
										//MemoryMarshal.AsBytes();
										//if(SpanHelper.IsPrimitive<T>())
										var str = token;
										var currObjInBytes = str;
										if (state is null /*|| saveState[comp.GetInstanceID()].ContainsKey("addedComponent")*)
											saveState[dirtyObj.GetInstanceID()] = ("changed", null, currObjInBytes);
										else
										{
											var prevObjInBytes = state;
											var delta2 = Delta.Create(currObjInBytes, prevObjInBytes);
											var delta1 = Delta.Create(prevObjInBytes, currObjInBytes);
											saveState[dirtyObj.GetInstanceID()] = ("changed", delta2, delta1);
										}
										prevStates.GetOrAddValueRef(dirtyObj) = token;
									}
								}
							}*/

				}
				if (saveState is not null && historyMoved is false)
				{
					SaveChanges(saveState);
					saveState = null;
				}
				historyMoved = false;
				Editor.isObjectsDirty = DirtyState.None;
			}

		}

		public static Vector3 ToEulerAngles(Quaternion q)
		{
			Vector3 angles = new Vector3();

			// roll (x-axis rotation)
			float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
			float cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
			angles.X = MathF.Atan2(sinr_cosp, cosr_cosp);

			// pitch (y-axis rotation)
			float sinp = 2 * (q.W * q.Y - q.Z * q.X);
			if (MathF.Abs(sinp) >= 1)
				angles.Y = MathF.CopySign(MathF.PI / 2, sinp); // use 90 degrees if out of range
			else
				angles.Z = MathF.Asin(sinp);

			// yaw (z-axis rotation)
			float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
			float cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
			angles.Z = MathF.Atan2(siny_cosp, cosy_cosp);

			return angles;
		}
		private static void SaveChanges(Dictionary<Guid, (string, byte[] undo, byte[] redo)> toBeSaved)
		{
			if (UndoCommand.currentHistory is not null && UndoCommand.currentHistory.Next is not null) //TODO: this is bugged state on split is doubled for some reason
			{
				UndoCommand.currentHistory.RemoveAllAfter();
				Console.WriteLine("clear trailing history");
			}
			var finalSave = new Dictionary<Guid, (string, byte[] undo, byte[] redo)>();
			foreach (var (index, val) in toBeSaved)
			{
				finalSave.Add(index, val);
			}
			UndoCommand.snapshots.AddLast(new History() { propertyMapping = finalSave });
			UndoCommand.currentHistory = UndoCommand.snapshots.Last;
		}
	}

}