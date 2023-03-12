using Sharp.Editor.UI;
using Squid;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Core.Editor.UI
{
	class Vector3Field : Control
	{
		private Func<Vector3> getter;
		private Action<Vector3> setter;
		private FlowLayoutFrame layout = new FlowLayoutFrame();
		private FloatField xField;
		private FloatField yField;
		private FloatField zField;
		public int precision = 9;

		/*public float Value
		{
			set
			{
				Text = value.ToString("R"/*"g17"*, CultureInfo.InvariantCulture.NumberFormat);
			}
			get
			{
				return float.TryParse(_text.AsSpan(), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out var x) ? x : 0;
			}
		}*/
		public Vector3Field(Func<Vector3> getter, Action<Vector3> setter)
		{
			layout.FlowDirection = FlowDirection.LeftToRight;
			layout.Position = new Point(0, 0);
			layout.HSpacing = 0;
			layout.AutoSize = AutoSize.HorizontalVertical;
			var tmpLabel = new Label();
			tmpLabel.Text = "X";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			xField = new FloatField(() => this.getter().X, (x) =>
			{
				var newVector = this.getter();
				newVector.X = x;
				this.setter(newVector);
			});

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(xField);

			tmpLabel = new Label();
			tmpLabel.Text = "Y";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			yField = new FloatField(() => this.getter().Y, (x) =>
			{
				var newVector = this.getter();
				newVector.Y = x;
				this.setter(newVector);
			});
			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(yField);

			tmpLabel = new Label();
			tmpLabel.Text = "Z";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			zField = new FloatField(() => this.getter().Z, (x) =>
			{
				var newVector = this.getter();
				newVector.Z = x;
				this.setter(newVector);
			});
			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(zField);
			this.getter = getter;
			this.setter = setter;
			Childs.Add(layout);
		}
	}
}
