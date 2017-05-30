using System;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI.Property
{
    internal class RangeDrawer : PropertyDrawer<object, RangeAttribute>
    {
        private HorizontalSlider slider;

        public RangeDrawer(Base parent) : base(parent)
        {
            slider = new HorizontalSlider(this);
            slider.SetRange(0, 100);
        }

        public override object Value
        {
            get { return slider.Value; }
            set { slider.Value = (float)value; }
        }
    }
}