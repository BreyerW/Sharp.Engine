using System;
using System.Globalization;
using Newtonsoft.Json.Linq;
using Squid;

namespace Sharp.Editor.UI
{
	//public delegate ref TResult RefFunc<TResult>();
	public class FloatField : TextField
	{
		private Func<float> getter;
		private Action<float> setter;
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
		public FloatField(Func<float> getter, Action<float> setter)
		{
			this.getter = getter;
			this.setter = setter;
			//Value = getter();
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
			this.TextChanged += FloatField_TextChanged;
		}

		private void FloatField_TextChanged(Control sender)
		{
			setter(Value);
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
