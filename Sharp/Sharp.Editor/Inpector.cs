using System;
using Gwen.Control;
using System.Collections.Generic;

namespace Sharp.Editor
{
	public abstract class Inpector<T>
	{
		//private static Dictionary<Type, Base> predefinedInspectors;
		internal static readonly T defaultObj=default(T);
		protected Func<T> target;

		public Inpector ()
		{
		}
		public abstract Base OnInitializeGUI();
	}
}

