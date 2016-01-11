using System;
using System.Runtime.InteropServices;


namespace Sharp
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Color<T> where T : struct, IConvertible
	{
		public T r;
		public T g;
		public T b;
		public T a;

		public Color(T R, T G, T B, T A){
			r = R;
			g = G;
			b = B;
			a = A;
		}
	}
}

