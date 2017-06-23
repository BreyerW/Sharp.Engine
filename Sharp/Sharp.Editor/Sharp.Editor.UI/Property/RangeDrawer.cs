using System;
using Gwen.Control;
using Gwen.Control.Property;
using System.Linq;
using Sharp.Editor.Attribs;

namespace Sharp.Editor.UI.Property
{
    public class RangeDrawer : PropertyDrawer<object>//valuetuple jako odpowiednik union?
    {
        private HorizontalSlider slider;

        public RangeDrawer(Base parent) : base(parent)
        {
            slider = new HorizontalSlider(this);
            var range = attributes?.OfType<RangeAttribute>();
            if (range != null && range.Any())
                slider.SetRange(range.GetEnumerator().Current.min, range.GetEnumerator().Current.max);
        }

        public override object Value
        {
            get { return slider.Value; }
            set { slider.Value = (float)value; }
        }
    }
}