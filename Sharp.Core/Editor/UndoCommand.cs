using Fossil;
//using Microsoft.Collections.Extensions;
using Sharp.Core;
using Sharp.Engine.Components;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp.Editor
{
	//maybe go back to string? otherwise might be difficult to support custom deltas
	public enum DeltaKind
	{
		AddedEntity,
		RemovedEntity,
		AddedComponent,
		RemovedComponent,
		AddedSystem,
		RemovedSystem,
		Changed
	}
	public struct History
	{
		public static bool isUndo = false;
		public static LinkedListNode<History> current;
		//TODO: switch guid to ulong if it will be supported in Interlocked, ulong is big enough 
		public Dictionary<DeltaKind, HashSet<(Guid id, byte[] undo, byte[] redo)>> propertyMapping;
	}

	public class UndoCommand : IMenuCommand
	{
		private static Dictionary<DeltaKind, HashSet<(Guid id, byte[] undo, byte[] redo)>> saveState = new();
		internal static Dictionary<IEngineObject, byte[]> prevStates = new();
		//internal static LinkedList<History> snapshots = new LinkedList<History>();

		//internal static LinkedListNode<History> currentHistory;
		public string menuPath => "Undo";
		internal static bool historyMoved = false;
		public string[] keyCombination => new[] { "CTRL", "z" };//combine into menuPath+(combination)

		public string Indentifier { get => menuPath; }

		//public static Stack<ICommand> done = new Stack<ICommand>();
		static UndoCommand()
		{
			Coroutine.Start(SaveChangesBeforeNextFrame());
		}
		public void Execute(bool reverse = true)
		{
			if (History.current.Previous is null)
				return;
			History.isUndo = true;
			historyMoved = true;

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.AddedEntity, out var deltas))
				HandleAddedEntities(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.RemovedEntity, out deltas))
				HandleRemovedEntities(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.AddedComponent, out deltas))
				HandleAddedComponents(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.RemovedComponent, out deltas))
				HandleRemovedComponents(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.AddedSystem, out deltas))
				HandleAddedSystems(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.RemovedComponent, out deltas))
				HandleRemovedSystems(deltas);

			if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.Changed, out deltas))
				HandleChanges(deltas);

			Coroutine.AdvanceInstructions<WaitForUndoRedo>();
			History.current = History.current.Previous;
			Squid.UI.isDirty = true;
		}
		private static void HandleAddedEntities(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, _, _) in deltas)
				index.GetInstanceObject<Entity>()?.Dispose();
		}
		private static void HandleRemovedEntities(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, undo, _) in deltas)
			{
				//var entity = RuntimeHelpers.GetUninitializedObject(typeof(Entity));//pseudodeserialization thats why we use this
			}
		}
		private static void HandleAddedSystems(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, undo, _) in deltas)
			{
				//componentsToBeAdded.Add(index.Item1, val);
			}
		}
		private static void HandleRemovedSystems(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, undo, _) in deltas)
			{
				//componentsToBeAdded.Add(index.Item1, val);
			}
		}
		private static void HandleAddedComponents(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, _, _) in deltas)
				index.GetInstanceObject<Component>()?.Dispose();
		}
		private static void HandleRemovedComponents(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, undo, _) in deltas)
			{
				//componentsToBeAdded.Add(index.Item1, val);
			}
		}
		private static void HandleChanges(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, undo, _) in deltas)
			{
				var obj = index.GetInstanceObject<IEngineObject>();
				ref var patched = ref CollectionsMarshal.GetValueRefOrNullRef(prevStates, obj);
				patched = Delta.Apply(patched, undo);
				PluginManager.serializer.Deserialize(patched, obj.GetType());
				if (obj is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
					startable.Start();
			}
		}
		//TODO: move to history struct?
		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (Selection.selectedAssets.changed)
					Selection.selectedAssets.RaiseEvent();
				if (historyMoved is false)
					foreach (var added in Root.addedEntities)
					{
						if (saveState is null)
							saveState = new();
						var bytes = PluginManager.serializer.Serialize(added, added.GetType());
						CollectionsMarshal.GetValueRefOrAddDefault(prevStates, added, out _) = bytes;
						if (added is Entity ent)
						{

							saveState.GetOrAdd(DeltaKind.AddedEntity).Add((ent.GetInstanceID(), null, bytes));
						}
						else if (added is Component comp)
						{
							//if (prevStates.TryGetValue(comp, out _)) throw new InvalidOperationException("unexpected add to already existing key");
							saveState.GetOrAdd(DeltaKind.AddedComponent).Add((comp.GetInstanceID(), null, bytes));
						}
					};
				foreach (var removed in Root.removedEntities)
					if (removed is IEngineObject comp)
						prevStates.Remove(comp);
				if (InputHandler.isKeyboardPressed || InputHandler.isMouseDragging)
					continue;

				//if(Editor.isObjectsDirty is DirtyState.All)
				foreach (var (comp, state) in prevStates)
				{
					var token = PluginManager.serializer.Serialize(comp, comp.GetType());

					if (!state.AsSpan().SequenceEqual(token.AsSpan()))
					{
						if (comp is Component c && c.Parent.GetComponent<Camera>() == Camera.main) continue;

						Console.WriteLine(" name " + /*Name + */" target " + comp);
						if (saveState is null)
							saveState = new();
						//MemoryMarshal.AsBytes();
						//if(SpanHelper.IsPrimitive<T>())
						var str = token;
						var currObjInBytes = str;
						if (state is null /*|| saveState[comp.GetInstanceID()].ContainsKey("addedComponent")*/)
							saveState.GetOrAdd(DeltaKind.Changed).Add((comp.GetInstanceID(), null, currObjInBytes));
						else
						{
							var prevObjInBytes = state;
							var delta2 = Delta.Create(currObjInBytes, prevObjInBytes);
							var delta1 = Delta.Create(prevObjInBytes, currObjInBytes);
							saveState.GetOrAdd(DeltaKind.Changed).Add((comp.GetInstanceID(), delta2, delta1));
						}
						CollectionsMarshal.GetValueRefOrAddDefault(prevStates, comp, out _) = token;
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

				SaveChanges(saveState);

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
		private static void SaveChanges(Dictionary<DeltaKind, HashSet<(Guid id, byte[] undo, byte[] redo)>> toBeSaved)
		{
			if (toBeSaved is null) return;
			if (History.current is { Next: not null }) //TODO: this is bugged state on split is doubled for some reason
			{
				History.current.RemoveAllAfter();
				Console.WriteLine("clear trailing history");
			}
			History.current = (History.current?.List ?? new LinkedList<History>()).AddLast(new History() { propertyMapping = toBeSaved });

			saveState = null;
		}
	}

}