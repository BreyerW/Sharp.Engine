using System;
using System.Runtime.InteropServices;
using Assimp;

namespace Sharp
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Color<T> where T : IConvertible
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

