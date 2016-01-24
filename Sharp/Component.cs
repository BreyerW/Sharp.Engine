using System;

namespace Sharp
{
	public abstract class Component
	{
		public bool active{get{return enabled; } set{
				if (enabled == value)
					return;
				enabled = value; 
				if (enabled)
					OnEnableInternal ();
				else
					OnDisableInternal ();}
		}
		private bool enabled;
		public Entity entityObject;

		protected internal virtual void OnEnableInternal (){}

		protected internal virtual void OnDisableInternal (){
		}

		public Component ()
		{
			active = true;
		}
	}
}

