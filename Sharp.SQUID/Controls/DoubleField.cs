using System;
using System.Globalization;

namespace Squid
{
	public class DoubleField : TextField
	{
		public double Value
		{
			set
			{
				_text = value.ToString("g17", CultureInfo.InvariantCulture.NumberFormat);
			}
			get
			{
				return double.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
			}
		}
		public int precision = 17;
		public DoubleField()
		{
			Value = default;
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
		}
		protected override void DrawText(Style style, float opacity, int charsToDraw)
		{
			charsToDraw = _text.AsSpan().IndexOf('.') + precision + 1;
			base.DrawText(style, opacity, charsToDraw);
		}
	}
}
