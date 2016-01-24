using System;
using Sharp;
using System.Collections.Generic;
using System.IO;

namespace Sharp
{
	public static class Selection
	{
		private static Stack<Func<object>> assets=new Stack<Func<object>>();
		public static Func<object> Asset{
			set{ 
				assets.Push (value);
				OnSelectionChange?.Invoke (value, EventArgs.Empty);
			}
			get{ 
				return assets.Peek ();
			}
		}
		public static EventHandler OnSelectionChange;
		public static bool isDragging=false;
	}
}

