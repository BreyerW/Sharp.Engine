using System;
using Sharp;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Antmicro.Migrant;
using System.Text;
using System.Threading;

namespace Sharp
{
    public static class Selection
    {
        private static Timer dirtyTimer = new Timer(IsSelectionDirty, nameof(IsDirty), 33, 33);
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
                serializer.Serialize(value, memStream.GetStream());
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

        private static void IsSelectionDirty(object obj)
        {
            if (Asset == null) return;

            serializer.Serialize(Asset, memStream.GetStream());
            var byteHash = md5.ComputeHash(memStream.GetStream().GetBuffer());
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
    }
}

