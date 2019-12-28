using System;
using System.Globalization;

namespace Squid
{
	public class IntField : TextField
	{
		public int Value
		{
			set
			{
				_text = value.ToString("d", CultureInfo.InvariantCulture.NumberFormat);
			}
			get
			{
				return int.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
			}
		}
		public int precision = 0;
		public IntField()
		{
			Value = default;
			Mode = TextBoxMode.Numeric;
			IsPassword = false;
		}
	}
}
