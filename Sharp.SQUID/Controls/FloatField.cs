using System;
using System.Globalization;

namespace Squid
{
	public class FloatField : TextField
	{
		public float Value
		{
			set
			{
				_text = value.ToString("g9", CultureInfo.InvariantCulture.NumberFormat);
			}
			get
			{
				return float.TryParse(_text, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
			}
		}
		public int precision = 9;
		public FloatField()
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
