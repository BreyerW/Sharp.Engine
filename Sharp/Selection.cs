using System;
using Sharp;
using System.Collections.Generic;
using System.IO;

namespace Sharp
{
	public static class Selection
	{
		private static MemoryStream inspectedObj;
		public static List<object> assets=new List<object>();
		public static bool isDragging=false;
	}
}

