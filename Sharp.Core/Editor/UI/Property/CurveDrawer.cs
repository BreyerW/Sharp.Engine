using Sharp.Editor.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property
{
	static partial class Registerer
	{
		[ModuleInitializer]
		internal static void Register1()
		{
			InspectorView.RegisterDrawerFor<Curve>(() => new CurveDrawer());
		}
	}
	public class CurveDrawer : PropertyDrawer<Curve>
	{
		/*[ModuleInitializer]
		internal static void Register()
		{
			//
		}*/
		public CurveDrawer() : base()
		{
		}

		//public override Curve Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	}
}