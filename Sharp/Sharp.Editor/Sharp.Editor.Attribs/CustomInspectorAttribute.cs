using System;
using System.Collections.Generic;

namespace Sharp.Editor.Attribs
{
	public class CustomInspectorAttribute:Attribute
	{
		public static HashSet<Type> typesToCustomize=new HashSet<Type>();

		public CustomInspectorAttribute (Type type)
		{
			if(!typesToCustomize.Contains(type))
			typesToCustomize.Add (type);
		}
	}
}

