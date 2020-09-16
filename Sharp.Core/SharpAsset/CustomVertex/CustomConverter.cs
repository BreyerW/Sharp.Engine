using System;
using System.Runtime.InteropServices;

namespace SharpAsset
{
	public static class CustomConverter
	{
		public static byte[] ToByteArray(ref object[] verts, int stride)
		{
			byte[] arr = new byte[stride*verts.Length];
			IntPtr ptr =Marshal.AllocHGlobal(stride);
			foreach(int i in ..verts.Length){
				Marshal.StructureToPtr(verts[i], ptr, true);
				Marshal.Copy(ptr, arr, stride*i,stride);
			}
			Marshal.FreeHGlobal(ptr);
			return arr;
		}
        public static IntPtr ToPtr(object[] verts, int stride)
        {
            IntPtr ptr = Marshal.AllocHGlobal(stride * verts.Length);
			foreach (int i in ..verts.Length)
			{
                Marshal.StructureToPtr(verts[i],IntPtr.Add(ptr, i*stride), false);
            }
            return ptr;
        }

    }
}

