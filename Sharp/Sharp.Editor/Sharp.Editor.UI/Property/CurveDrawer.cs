using System;
using System.Collections.Generic;
using System.Linq;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI.Property
{
    public class CurveDrawer : PropertyDrawer<Curve>
    {
        public CurveDrawer(Base parent) : base(parent)
        {
        }

        public override Curve Value { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}