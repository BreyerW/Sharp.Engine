using System;
using Sharp;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Antmicro.Migrant;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sharp
{
    public static class Selection
    {
        private static Stack<object> assets = new Stack<object>();
        private static Serializer serializer = new Serializer();
        private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
        private static MD5 md5 = MD5.Create();//maybe sha instead
        internal static string lastHash;

        public static object Asset
        {
            set
            {
                if (value == Asset) return;
                assets.Push(value);
                //Thread.SetData
                OnSelectionChange?.Invoke(value, EventArgs.Empty);
            }
            get
            {
                if (assets.Count == 0)
                    return null;
                return assets.Peek();
            }
        }

        public static EventHandler OnSelectionChange;
        public static EventHandler OnSelectionDirty;
        public static bool isDragging = false;
        public static bool IsDirty = false;

        static Selection()
        {
            Repeat(IsSelectionDirty, 33, 33, CancellationToken.None);
        }

        private static void IsSelectionDirty(CancellationToken token)
        {
            var asset = Asset;
            if (asset == null) return;
            var buffer = memStream.GetStream();
            serializer.Serialize(asset, buffer);
            var byteHash = md5.ComputeHash(buffer.GetBuffer());
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < byteHash.Length; i++)
            {
                sb.Append(byteHash[i].ToString("X2"));
            }

            var currentHash = sb.ToString();
            if (currentHash != lastHash)
                IsDirty = true;
            lastHash = currentHash;
        }
        public static async Task Repeat(Action<CancellationToken> doWork, int delayInMilis, int periodInMilis, CancellationToken cancellationToken, bool singleThreaded = false)
        {
            await Task.Delay(delayInMilis, cancellationToken).ConfigureAwait(singleThreaded);
            while (!cancellationToken.IsCancellationRequested)
            {
                doWork(cancellationToken);
                await Task.Delay(periodInMilis, cancellationToken).ConfigureAwait(singleThreaded);
            }
        }
    }
}

