using Sharp.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp
{//waitforYndoRedo
	public class ListWrapper : IEngineObject, IList
	{
		public static Action<List<object>> OnListChange;
		[field: JsonInclude]
		private List<object> List { get; set; } = new();
		[JsonIgnore]
		public int Count => List.Count;

		[JsonIgnore]
		public bool changed = false;
		[JsonIgnore]
		public bool IsFixedSize => ((IList)List).IsFixedSize;
		[JsonIgnore]
		public bool IsReadOnly => ((IList)List).IsReadOnly;
		[JsonIgnore]
		public bool IsSynchronized => ((ICollection)List).IsSynchronized;
		[JsonIgnore]
		public object SyncRoot => ((ICollection)List).SyncRoot;

		public object this[int index] { get => ((IList)List)[index]; set { ((IList)List)[index] = value; changed = true; } }

		public void Clear()
		{
			List.Clear();
			changed = true;
		}

		public bool Contains(object item)
		{
			return List.Contains(item);
		}

		public void CopyTo(object[] array, int arrayIndex)
		{
			List.CopyTo(array, arrayIndex);
		}

		public IEnumerator GetEnumerator()
		{
			return List.GetEnumerator();
		}
		public void ReplaceWithOne(object item)
		{
			List.Clear();
			List.Add(item);
			changed = true;
		}

		public int IndexOf(object item)
		{
			return ((IList)List).IndexOf(item);
		}

		public void Insert(int index, object item)
		{
			((IList)List).Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			((IList)List).RemoveAt(index);
		}

		public int Add(object value)
		{
			return ((IList)List).Add(value);
		}

		void IList.Remove(object value)
		{
			((IList)List).Remove(value);
			changed = true;
		}

		public void CopyTo(Array array, int index)
		{
			((ICollection)List).CopyTo(array, index);
		}
		public void RaiseEventAndReset()
		{
			changed = false;
			OnListChange?.Invoke(List);
		}
	}

	public static class Selection//enimachine engine
	{
		public static ListWrapper selectedAssets = new();

		public static object sync = new object();
		/*public static HashSetWrapper<object> Assets
		{
			get => assets;
			set => assets = value;
		}*/
		/*public static object Asset
		{
			set
			{
				if (value == Asset) return;
				OnSelectionChange?.Invoke(Asset, value);
				selectedAssets.Clear();
				if (value is not null)
					selectedAssets.Add(value);
				//Thread.SetData

			}
			get
			{
				if (selectedAssets.Count == 0)
					return null;
				return selectedAssets.FirstOrDefault();
			}
		}*/
		public static object HoveredObject
		{
			get; set;
		}
		//public static Action<object> OnSelectionDirty;
		public static bool isDragging = false;


		static Selection()
		{
			Extension.entities.AddEngineObject(selectedAssets);
			Coroutine.Start(CheckForSelectionUndoRedo());
		}
		public static async Task Repeat(Action<CancellationToken> doWork, int delayInMilis, int periodInMilis, CancellationToken cancellationToken, bool singleThreaded = false)
		{
			await Task.Delay(delayInMilis, cancellationToken).ConfigureAwait(singleThreaded);
			while (!cancellationToken.IsCancellationRequested)
			{
				//waiter.Wait(delayInMilis, cancellationToken);
				doWork(cancellationToken);
				//waiter.Reset();
				await Task.Delay(periodInMilis, cancellationToken).ConfigureAwait(singleThreaded);
			}
		}
		private static IEnumerator<object> CheckForSelectionUndoRedo()
		{
			while (true)
			{
				yield return new WaitForUndoRedo();
				/*if (History.current.Value.propertyMapping.TryGetValue(DeltaKind.AddedSelection, out var delta))
				{
					if (History.isUndo)
						foreach (var (index, undo, _) in delta)
						{
							var obj = undo is null ? null : new Guid(undo).GetInstanceObject();
							if (obj is not null)
								assets.Push(obj);
							else
								assets.Clear();
						}
					else
					{

					}
				}*/
			}
		}
	}
}