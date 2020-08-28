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
			this.Layout += (evt) =>
			{
				if (fl is null)
				{
					fl = new FloatField(SerializedObject);
					fl.TextChanged += Fl_TextChanged;
					Childs.Add(fl);
				}
			};
		}

		private void Fl_TextChanged(Squid.Control sender)
		{
			ApplyChanges();
		}
	}
}
