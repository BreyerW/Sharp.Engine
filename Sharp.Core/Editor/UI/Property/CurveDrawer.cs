using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	public class CurveDrawer : PropertyDrawer<Curve>
	{
		public CurveDrawer(MemberInfo memInfo) : base(memInfo)
		{
		}

		//public override Curve Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	}
}