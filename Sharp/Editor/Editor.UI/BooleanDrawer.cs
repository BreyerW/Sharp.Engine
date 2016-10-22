using System;
using System.Collections.Generic;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI
{
	class BooleanDrawer : PropertyDrawer<bool>
	{
		public override void OnInitializeGUI(Properties propertyTree, string propertyName, object propertyValue, Action<object> setter)
		{
			var field = propertyTree.Get<bool>(propertyName + ":");
			if (field == null)
				field = propertyTree.Add(propertyName + ":", new Check(propertyTree));
			field.Value = (bool)propertyValue;
			field.ValueChanged += (o, args) =>
			{
				var tmpObj = o as PropertyRow<bool>;
				setter(tmpObj.Value);
			};
		}
	}
}
