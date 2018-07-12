using Sharp.Editor.UI.Property;

namespace Sharp.Editor
{
	public abstract class ComponentDrawer<T>//SelectionDrawer
	{
		//private static Dictionary<Type, Base> predefinedInspectors;
		internal T getTarget;

		//internal Func<T> setTarget;
		public ComponentNode properties;

		public T Target
		{
			get { return getTarget; }
			//set{setTarget (value);}
		}

		/*public ref T Target
        {
            get { return ref getTarget; }
            //set{setTarget (value);}
        }*/

		public ComponentDrawer()
		{
		}

		public abstract void OnInitializeGUI();
	}
}