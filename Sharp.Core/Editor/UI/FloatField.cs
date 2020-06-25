using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Squid;

namespace Sharp.Editor.UI
{
	public class FloatField : TextField
	{
		private string loc;
		private JToken property;
		public int precision = 9;
		public FloatField(JToken prop)
		{
			loc = prop.Path;
			property = prop.Parent is JProperty p ? p.Parent : prop.Parent;
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
			this.TextChanged += FloatField_TextChanged;
		}

		private void FloatField_TextChanged(Control sender)
		{
			property[loc] = _text;
		}

		protected override void DrawText(Style style, float opacity, int charsToDraw)
		{
			_text = property[loc].Value<string>();//set top (null parent but possibly bad idea for jvalue?) JToken dirty and here check if property is dirty this will avoid unnecessary deserializations
			charsToDraw = _text.AsSpan().IndexOf('.') + precision + 1;
			base.DrawText(style, opacity, charsToDraw);
		}
	}
}
