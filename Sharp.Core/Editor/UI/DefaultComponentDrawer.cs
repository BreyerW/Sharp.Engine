using Sharp.Editor.UI.Property;
using Sharp.Editor.Views;
using System;
using System.Linq;
using System.Reflection;

namespace Sharp.Editor.UI
{
	public class DefaultComponentDrawer : ComponentDrawer<Component>
	{
		public override void OnInitializeGUI()//OnSelect
		{
			var props = Target.GetType().GetFields().Where(p => !p.IsStatic);//.Where(p => p.CanRead && (p.CanWrite || p.PropertyType.IsByRef));
																			 //var props = Target.GetType().GetFields(BindingFlags.Instance|BindingFlags.NonPublic);
			foreach (var prop in props)
			{
				Console.WriteLine(prop.Name);
				if (prop.GetCustomAttribute<NonSerializableAttribute>(false) != null) continue;

				var propDrawer = InspectorView.Add(prop);
				if (propDrawer is null)
					continue;
				propDrawer.Target = Target;
				Frame.Controls.Add(propDrawer);
			}
		}
	}
}