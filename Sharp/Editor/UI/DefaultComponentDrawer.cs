using Sharp.Editor.Views;
using System;
using System.Linq;
using System.Reflection;

namespace Sharp.Editor.UI
{
	//[CustomInspector(typeof(object))]
	public class DefaultComponentDrawer : ComponentDrawer<object>
	{
		public override void OnInitializeGUI()//OnSelect
		{
			var props = Target.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);
			//var props = Target.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic);
			foreach (var prop in props)
			{
				if (prop.GetCustomAttribute<NonSerializableAttribute>(false) != null) continue;

				var propDrawer = InspectorView.Add(prop.Name + ":", Target, prop);

				if (propDrawer != null)
					properties.Frame.Controls.Add(propDrawer);
			}
		}
	}
}