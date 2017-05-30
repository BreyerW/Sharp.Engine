using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace fastBinaryJSON
{
    internal sealed class SafeDictionary<TKey, TValue>
    {
        private readonly object _Padlock = new object();
        private readonly Dictionary<TKey, TValue> _Dictionary = new Dictionary<TKey, TValue>();

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_Padlock)
                return _Dictionary.TryGetValue(key, out value);
        }

        public TValue this[TKey key]
        {
            get
            {
                lock (_Padlock)
                    return _Dictionary[key];
            }
            set
            {
                lock (_Padlock)
                    _Dictionary[key] = value;
            }
        }

        public int Count { get { lock (_Padlock) return _Dictionary.Count; } }

        public void Add(TKey key, TValue value)
        {
            lock (_Padlock)
            {
                if (_Dictionary.ContainsKey(key) == false)
                    _Dictionary.Add(key, value);
            }
        }
    }

    internal static class Helper
    {
        internal static int ToInt32(byte[] value, int startIndex, bool reverse)
        {
            if (reverse)
            {
                byte[] b = new byte[4];
                Unsafe.CopyBlock(ref value[startIndex], ref b[0], 4);
                //Buffer.BlockCopy(value, startIndex, b, 0, 4);
                Array.Reverse(b);
                return ToInt32(b, 0);
            }

            return ToInt32(value, startIndex);
        }

        internal static int ToInt32(byte[] value, int startIndex)
        {
            return Unsafe.As<byte, int>(ref value[startIndex]);
            /* fixed (byte* numRef = &(value[startIndex]))
             {
                 return *((int*)numRef);
             }*/
        }

        internal static long ToInt64(byte[] value, int startIndex, bool reverse)
        {
            if (reverse)
            {
                byte[] b = new byte[8];
                Unsafe.CopyBlock(ref value[startIndex], ref b[0], 8);
                //Buffer.BlockCopy(value, startIndex, b, 0, 8);
                Array.Reverse(b);
                return ToInt64(b, 0);
            }
            return ToInt64(value, startIndex);
        }

        internal static long ToInt64(byte[] value, int startIndex)
        {
            return Unsafe.As<byte, long>(ref value[startIndex]);
            /* fixed (byte* numRef = &(value[startIndex]))
             {
                 return *(((long*)numRef));
             }*/
        }

        internal static short ToInt16(byte[] value, int startIndex, bool reverse)
        {
            if (reverse)
            {
                byte[] b = new byte[2];
                Unsafe.CopyBlock(ref value[startIndex], ref b[0], 2);
                //Buffer.BlockCopy(value, startIndex, b, 0, 2);
                Array.Reverse(b);
                return ToInt16(b, 0);
            }
            return ToInt16(value, startIndex);
        }

        internal static short ToInt16(byte[] value, int startIndex)
        {
            return Unsafe.As<byte, short>(ref value[startIndex]);
            /* fixed (byte* numRef = &(value[startIndex]))
             {
                 return *(((short*)numRef));
             }*/
        }

        internal static byte[] GetBytes(long num, bool reverse)
        {
            byte[] buffer = new byte[8];
            Unsafe.WriteUnaligned(ref buffer[0], num);
            /*fixed (byte* numRef = buffer)
            {
                *((long*)numRef) = num;
            }*/
            if (reverse)
                Array.Reverse(buffer);
            return buffer;
        }

        public static byte[] GetBytes(int num, bool reverse)
        {
            byte[] buffer = new byte[4];
            Unsafe.WriteUnaligned(ref buffer[0], num);
            /* fixed (byte* numRef = buffer)
             {
                 *((int*)numRef) = num;
             }*/
            if (reverse)
                Array.Reverse(buffer);
            return buffer;
        }
    }
}