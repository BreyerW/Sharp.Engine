using Squid;
using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	public class Vector3Drawer : PropertyDrawer<Vector3>
	{
		private FlowLayoutFrame layout = new FlowLayoutFrame();
		private FloatField posX;
		private FloatField posY;
		private FloatField posZ;

		public Vector3Drawer(string name) : base(name)
		{
			layout.FlowDirection = FlowDirection.LeftToRight;
			layout.Position = new Point(label.Size.x, 0);
			layout.HSpacing = 0;
			layout.AutoSize = AutoSize.HorizontalVertical;
			//AutoSize = AutoSize.HorizontalVertical;
			//layout.Scissor = false;
			posX = new FloatField();
			posX.precision = Application.roundingPrecision;
			var tmpLabel = new Label();
			tmpLabel.Text = "X";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posX);

			posY = new FloatField();
			posY.precision = Application.roundingPrecision;
			tmpLabel = new Label();
			tmpLabel.Text = "Y";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posY);

			posZ = new FloatField();
			posZ.precision = Application.roundingPrecision;
			tmpLabel = new Label();
			tmpLabel.Text = "Z";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posZ);

			Childs.Add(layout);
		}

		public override Vector3 Value
		{
			get
			{
				return new Vector3(posX.Value, posY.Value, posZ.Value);
			}
			set
			{
				posX.Value = value.X; //Math.Round(value.X, ).ToString();
				posY.Value = value.Y;//Math.Round(value.Y, Application.roundingPrecision).ToString();
				posZ.Value = value.Z; //Math.Round(value.Z, Application.roundingPrecision).ToString();
			}
		}
	}
}