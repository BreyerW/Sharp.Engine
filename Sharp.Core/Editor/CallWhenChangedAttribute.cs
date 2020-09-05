using System;
using System.Collections.Generic;
using System.Text;

namespace Sharp.Core.Editor
{
	class CallWhenChangedAttribute : Attribute
	{
		public CallWhenChangedAttribute(params string[] methods)
		{

		}
	}
}
