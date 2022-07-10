using Fossil;
using Sharp.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
	public struct History
	{
		//internal static LinkedList<History> snapshots = new LinkedList<History>();

		//internal static LinkedListNode<History> currentHistory;
		public static bool isUndo = false;
		public static LinkedListNode<History> current;
		private static HashSet<(Guid id, byte[] undo, byte[] redo)> saveState = new();
		internal static Dictionary<IEngineObject, byte[]> prevStates = new();
		//TODO: switch guid to ulong if it will be supported in Interlocked, ulong is big enough 
		public HashSet<(Guid id, byte[] undo, byte[] redo)> propertyMapping;

		static History()
		{
			Coroutine.Start(SaveChangesBeforeNextFrame());
		}

		//TODO: move to history struct?
		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (Selection.selectedAssets.changed)
					Selection.selectedAssets.RaiseEventAndReset();

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
						if (state is null)
							saveState.Add((comp.GetInstanceID(), null, currObjInBytes));
						else
						{
							var prevObjInBytes = state;
							var delta2 = Delta.Create(currObjInBytes, prevObjInBytes);
							var delta1 = Delta.Create(prevObjInBytes, currObjInBytes);
							saveState.Add((comp.GetInstanceID(), delta2, delta1));
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

				Editor.isObjectsDirty = DirtyState.None;
			}

		}
		private static void SaveChanges(HashSet<(Guid id, byte[] undo, byte[] redo)> toBeSaved)
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