using System;
using Gwen.Control;
using System.Collections.Generic;

namespace Sharp.Editor
{
	public abstract class Inpector<T>
	{
		//private static Dictionary<Type, Base> predefinedInspectors;
		internal Func<T> getTarget;
		//internal Func<T> setTarget;
		public Properties properties;

		public T Target {
			get{return getTarget ();}
			//set{setTarget (value);}
		}

		public Inpector ()
		{
		}
		public abstract void OnInitializeGUI();
	}
}

