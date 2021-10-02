using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Core
{
    public readonly struct Ptr
    {
        private readonly static HashSet<IntPtr> activeUnmanagedMemories = new HashSet<IntPtr>();
        private readonly IntPtr ptr;
        public readonly bool IsFreed => !activeUnmanagedMemories.Contains(ptr);
        public readonly IntPtr Length
        {
            get
            {
                unsafe
                {
                    return Unsafe.AsRef<IntPtr>(ptr.ToPointer());
                }
            }
        }
        public readonly Span<T> GetDataAs<T>() where T : unmanaged
        {
            unsafe
            {
                var addr = IntPtr.Add(ptr, IntPtr.Size);
                return new Span<T>(addr.ToPointer(), (int)Length);
            }
        }
        public Ptr(IntPtr lengthInBytes, IntPtr existingMemory = default)
        {
            if (existingMemory == default)
            {
                ptr = Marshal.AllocHGlobal(lengthInBytes + IntPtr.Size);
                Marshal.WriteIntPtr(ptr, lengthInBytes);
                activeUnmanagedMemories.Add(ptr);
            }
            else
                ptr = existingMemory;
        }
        public readonly void Free()
        {
            activeUnmanagedMemories.Remove(ptr);
            Marshal.FreeHGlobal(ptr);
        }
    }
}
