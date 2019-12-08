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
			var props = Target.GetType().GetProperties().Where(p => p.CanRead && (p.CanWrite || p.PropertyType.IsByRef));
			//var props = Target.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic);
			foreach (var prop in props)
			{
				Console.WriteLine(prop.Name);
				if (prop.GetCustomAttribute<NonSerializableAttribute>(false) != null) continue;

				var propDrawer = InspectorView.Add(prop);
				propDrawer.target = Target as Component;
				if (propDrawer != null)
					properties.Frame.Controls.Add(propDrawer);
			}
		}
	}
}