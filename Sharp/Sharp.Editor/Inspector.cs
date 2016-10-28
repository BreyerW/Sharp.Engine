using System;
using Gwen.Control;
using System.Collections.Generic;

namespace Sharp.Editor
{
	public abstract class Inspector<T>
	{
		//private static Dictionary<Type, Base> predefinedInspectors;
		internal T getTarget;
		//internal Func<T> setTarget;
		public Properties properties;

		public T Target {
			get{ return getTarget; }
			//set{setTarget (value);}
		}

        /*public ref T Target
        {
            get { return ref getTarget; }
            //set{setTarget (value);}
        }*/
        public Inspector ()
		{
		}
		public abstract void OnInitializeGUI();
	}
}

