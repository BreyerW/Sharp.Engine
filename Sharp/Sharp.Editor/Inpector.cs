using System;
using Gwen.Control;
using System.Collections.Generic;

namespace Sharp.Editor
{
	public abstract class Inpector
	{
		private static Dictionary<Type, Base> predefinedInspectors;

		protected object target;

		public Inpector ()
		{
		}
		public abstract Base OnInitializeGUI();
	}
}

