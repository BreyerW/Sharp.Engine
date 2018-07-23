using System;
using System.Threading;

namespace Sharp
{
	public static class Extension
	{
	}

	public static class Utils
	{
		public static void Swap<T>(ref T v1, ref T v2) where T : class
		{
			var prev = Interlocked.Exchange(ref v1, v2);
			Interlocked.Exchange(ref v2, prev);
		}
	}
}