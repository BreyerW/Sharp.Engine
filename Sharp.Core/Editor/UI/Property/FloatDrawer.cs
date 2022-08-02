using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sharp.Core.Editor.UI.Property
{
	static partial class Registerer
	{
		[ModuleInitializer]
		internal static void Register3()
		{
			InspectorView.RegisterDrawerFor<float>(() => new FloatDrawer());
		}
	}
	class FloatDrawer : PropertyDrawer<float>
	{
		private FloatField fl;

		public FloatDrawer() : base()
		{

			fl = new FloatField(() => (float)Value, (x) => Value = x);
			Childs.Add(fl);
		}
	}
}
