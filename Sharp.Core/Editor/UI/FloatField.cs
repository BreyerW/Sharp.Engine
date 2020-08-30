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
			property = prop switch
			{
				{ Parent: JProperty p } => p.Parent,
				{ Parent: null } => prop,
				_ => prop.Parent
			};
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
			this.TextChanged += FloatField_TextChanged;
		}

		private void FloatField_TextChanged(Control sender)
		{
			if (loc is "")
				property = _text;
			else
				property[loc] = _text;
		}

		protected override void DrawText(Style style, float opacity, int charsToDraw)
		{

			_text = (loc is "" ? property : property[loc]).Value<string>();//set top (null parent but possibly bad idea for jvalue?) JToken dirty and here check if property is dirty this will avoid unnecessary deserializations
			var dotPos = _text.AsSpan().IndexOf('.');
			charsToDraw = dotPos is -1 ? _text.Length : _text.AsSpan().IndexOf('.') + precision + 1;
			base.DrawText(style, opacity, charsToDraw);
		}
	}
}
