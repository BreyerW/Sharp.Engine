using System;
using System.Collections.Generic;
using Gwen.Control;
using Sharp.Editor.Attribs;
using OpenTK;

namespace Sharp.Editor
{
	[CustomInspector(typeof(object))]
	public class DefaultInspector:Inpector
	{
		public DefaultInspector ()
		{
		}
		public override Base OnInitializeGUI ()
		{
			throw new NotImplementedException ();
		}
	}
}

