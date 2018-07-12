using Sharp.Editor.Attribs;
using Squid;
using System.Linq;

namespace Sharp.Editor.UI.Property
{
	public class RangeDrawer : PropertyDrawer<object>//valuetuple jako odpowiednik union?
	{
		private Slider slider;

		public RangeDrawer(string name) : base(name)
		{
			slider = new Slider();
			var range = attributes?.OfType<RangeAttribute>();
			//if (range != null && range.Any())
			//  slider.SetRange(range.GetEnumerator().Current.min, range.GetEnumerator().Current.max);
		}

		public override object Value
		{
			get { return slider.Value; }
			set { slider.Value = (float)value; }
		}
	}
}