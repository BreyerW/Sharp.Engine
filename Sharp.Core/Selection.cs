using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp
{
    public static class Selection//enimachine engine
    {
        private static Stack<object> assets = new Stack<object>();

        public static object sync = new object();
        public static Stack<object> Assets
        {
            get => assets;
            set => assets = value;
        }
        public static object Asset
        {
            set
            {
                if (value == Asset) return;
                OnSelectionChange?.Invoke(Asset, value);
                assets.Clear();
                if (value is not null)
                    assets.Push(value);
                //Thread.SetData

            }
            get
            {
                if (assets.Count == 0)
                    return null;
                return assets.Peek();
            }
        }
        public static object HoveredObject
        {
            get; set;
        }
        public static Action<object, object> OnSelectionChange;
        public static Action<object> OnSelectionDirty;
        public static bool isDragging = false;

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

        public static void RemoveAllBefore<T>(this LinkedListNode<T> node)
        {
            while (node.Previous != null) node.List.Remove(node.Previous);
        }

        public static void RemoveAllAfter<T>(this LinkedListNode<T> node)
        {
            while (node.Next != null) node.List.Remove(node.Next);
        }
    }
}