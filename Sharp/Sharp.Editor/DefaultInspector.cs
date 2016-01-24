using System;
using System.Collections.Generic;
using Gwen.Control;
using Sharp.Editor.Attribs;
using System.Linq;

namespace Sharp.Editor
{
	//[CustomInspector(typeof(object))]
	public class DefaultInspector:Inpector<object>
	{
		public override void OnInitializeGUI ()
		{
			var props=Target.GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);
			foreach (var prop in props) {
				//if(prop.Name!="active")
				properties.Add (prop.Name+":",new Gwen.Control.Property.Text(properties),prop.GetValue(Target).ToString()).ValueChanged+=(o,arg)=>{var tmpObj=o as PropertyRow<string>; prop.SetValue(Target,tmpObj.Value);};

			}
		}
	}
}

