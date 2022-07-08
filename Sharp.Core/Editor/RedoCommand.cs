using Fossil;
using Sharp.Core;
using Sharp.Engine.Components;
using Sharp.Serializer;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp.Editor
{
	public class RedoCommand : IMenuCommand
	{
		public string menuPath => "Redo";

		public string[] keyCombination => new[] { "CTRL", "SHIFT", "z" };

		public string Indentifier { get => menuPath; }

		public void Execute(bool reverse = false)//TODO: add object context parameter with anything under mouse?
		{
			if (History.current.Next is null)
				return;
			UndoCommand.historyMoved = true;
			History.isUndo = false;

			History.current = History.current.Next;
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
			//TODO: add pointer type with IntPtr and size and PointerConverter where serializer will regenerate unmanaged memory automatcally or abuse properties with internal set or prepend unmanaged memory with size then IntPtr can be used as is
			Squid.UI.isDirty = true;
		}
		private static void HandleAddedEntities(HashSet<(Guid, byte[], byte[])> deltas)
		{
			foreach (var (index, _, redo) in deltas)
			{
				var entity = PluginManager.serializer.Deserialize(redo, typeof(Entity)) as Entity;

				entity.components = new List<Component>();
				entity.childs = new List<Entity>();
				CollectionsMarshal.GetValueRefOrAddDefault(UndoCommand.prevStates, entity, out _) = redo;
				Extension.entities.AddRestoredEngineObject(entity, index);
			}
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
			foreach (var (index, _, redo) in deltas)
			{
				var component = PluginManager.serializer.Deserialize(redo, IdReferenceResolver.guidToTypeMapping[index]) as Component;
				CollectionsMarshal.GetValueRefOrAddDefault(UndoCommand.prevStates, component, out _) = redo;
			}
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
			foreach (var (index, _, redo) in deltas)
			{
				ref var patched = ref CollectionsMarshal.GetValueRefOrNullRef(UndoCommand.prevStates, index.GetInstanceObject<IEngineObject>());
				patched = Delta.Apply(patched, redo);
				var obj = index.GetInstanceObject<IEngineObject>();
				PluginManager.serializer.Deserialize(patched, obj.GetType());
				if (obj is IStartableComponent startable)//TODO: change to OnDeserialized callaback when System.Text.Json will support it
					startable.Start();
				//Console.WriteLine(object.ReferenceEquals(index.GetInstanceObject(),UndoCommand.prevStates.FirstOrDefault((obj) => obj.Key.GetInstanceID() == index).Key));
			}
		}
	}
}