using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Sharp.Core.Editor.UI.Property
{
	class FloatDrawer : PropertyDrawer<float>
	{
		private FloatField fl;

		public FloatDrawer(MemberInfo memInfo) : base(memInfo)
		{

			fl = new FloatField(() => Value, (x) => Value = x);
			Childs.Add(fl);
		}
	}
}
