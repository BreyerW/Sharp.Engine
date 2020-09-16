using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Squid;

namespace Sharp.Editor.UI
{
	public delegate ref TResult RefFunc<TResult>();
	public class FloatField : TextField
	{
		private RefFunc<float> getter;
		public int precision = 9;
		public float Value
		{
			set
			{
				Text = value.ToString("R"/*"g17"*/, CultureInfo.InvariantCulture.NumberFormat);
			}
			get
			{
				return float.TryParse(_text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
			}
		}
		public FloatField(RefFunc<float> getter)
		{
			this.getter = getter;
			//Value = getter();
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
			this.TextChanged += FloatField_TextChanged;
		}

		private void FloatField_TextChanged(Control sender)
		{
			getter() = Value;
		}

		protected override void DrawText(Style style, float opacity, int charsToDraw)
		{
			var dotPos = _text.AsSpan().IndexOf('.');
			charsToDraw = dotPos is -1 ? _text.Length : _text.AsSpan().IndexOf('.') + precision + 1;
			base.DrawText(style, opacity, charsToDraw);
		}
		protected override void OnLateUpdate()
		{
			Value = getter();
		}
	}
}
