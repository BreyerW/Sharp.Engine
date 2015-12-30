using System;
using Sharp;
using System.Collections.Generic;
using System.IO;

namespace Sharp
{
	public static class Selection
	{
		public static Stack<Func<object>> assets=new Stack<Func<object>>();
		public static bool isDragging=false;
	}
}

