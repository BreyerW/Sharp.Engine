using System;
using System.Collections.Generic;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI
{
	class Vector3Drawer : PropertyDrawer<OpenTK.Vector3>
	{
		public override void OnInitializeGUI(Properties propertyTree, string propertyName, object propertyValue, Action<object> setter)
		{
			var field = propertyTree.Get<OpenTK.Vector3>(propertyName + ":");
			if (field == null)
				field = propertyTree.Add(propertyName + ":", new Vector3(propertyTree));
			field.Value = (OpenTK.Vector3)propertyValue;
			field.ValueChanged += (o, args) =>
			{
				var tmpObj = o as PropertyRow<OpenTK.Vector3>;
				setter(tmpObj.Value);
			};

		}
	}
}
