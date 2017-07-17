using System;
using Sharp;
using System.Collections.Generic;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using fastBinaryJSON;
using Squid;

//using UniversalSerializerLib3;

namespace Sharp
{
    public static class Selection//enimachine engine
    {
        private static Stack<object> assets = new Stack<object>();

        //private static UniversalSerializer serializer = new UniversalSerializer(new Parameters());
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

        static Selection()
        {
            Repeat(IsSelectionDirty, 30, 30, CancellationToken.None);
        }

        public static void IsSelectionDirty(CancellationToken token)
        {
            var asset = Asset;
            if (asset == null) return;

            BJSON.RegisterCustomType(typeof(EventHandler), (obj) => "", (obj) => new EventHandler((o, args) => { }));

            var watch = System.Diagnostics.Stopwatch.StartNew();
            var data = BJSON.ToBJSON(asset);
            watch.Stop();
            //Console.WriteLine("cast: " + watch.ElapsedTicks);
            var byteHash = md5.ComputeHash(data);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < byteHash.Length; i++)
            {
                sb.Append(byteHash[i].ToString("X2"));
            }
            var currentHash = sb.ToString();
            if (currentHash != lastHash)
                UI.isDirty = true;
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

    internal class Size<TT>
    {
        private readonly TT _obj;
        private readonly HashSet<object> references;

        private static readonly int PointerSize =
        Environment.Is64BitOperatingSystem ? sizeof(long) : sizeof(int);

        public Size(TT obj)
        {
            _obj = obj;
            references = new HashSet<object>() { _obj };
        }

        public long GetSizeInBytes()
        {
            return this.GetSizeInBytes(_obj);
        }

        // The core functionality. Recurrently calls itself when an object appears to have fields
        // until all fields have been  visited, or were "visited" (calculated) already.
        private long GetSizeInBytes<T>(T obj)
        {
            if (obj == null) return sizeof(int);
            var type = obj.GetType();
            //RuntimeTypeHandle th = asset.GetType().TypeHandle;

            //Console.WriteLine(Marshal.ReadInt32(th.Value, 4));
            if (type.IsPrimitive)
            {
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Boolean:
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                        return sizeof(byte);

                    case TypeCode.Char:
                        return sizeof(char);

                    case TypeCode.Single:
                        return sizeof(float);

                    case TypeCode.Double:
                        return sizeof(double);

                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                        return sizeof(Int16);

                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        return sizeof(Int32);

                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    default:
                        return sizeof(Int64);
                }
            }
            else if (obj is decimal)
            {
                return sizeof(decimal);
            }
            else if (obj is string)
            {
                return sizeof(char) * obj.ToString().Length;
            }
            else if (type.IsEnum)
            {
                return sizeof(int);
            }
            else if (type.IsArray)
            {
                long size = PointerSize;
                var casted = (IEnumerable)obj;
                foreach (var item in casted)
                {
                    size += GetSizeInBytes(item);
                }
                return size;
            }
            else if (obj is System.Reflection.Pointer)
            {
                return PointerSize;
            }
            else
            {
                long size = 0;
                var t = type;
                while (t != null)
                {
                    size += PointerSize;
                    var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    foreach (var field in fields)
                    {
                        var tempVal = field.GetValue(obj);
                        if (!references.Contains(tempVal))
                        {
                            references.Add(tempVal);
                            size += this.GetSizeInBytes(tempVal);
                        }
                    }
                    t = t.BaseType;
                }
                return size;
            }
        }
    }
}