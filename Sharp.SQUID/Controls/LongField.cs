using System;
using System.Globalization;

namespace Squid
{
    public class LongField : TextField
    {
        public long Value
        {
            set
            {
                _text = value.ToString("d", CultureInfo.InvariantCulture.NumberFormat);
            }
            get
            {
                return long.TryParse(_text, NumberStyles.Integer, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
            }
        }
        public int precision = 0;
        public LongField()
        {
            Value = default;
            Mode = TextBoxMode.Numeric;
            IsPassword = false;
        }
    }
}
