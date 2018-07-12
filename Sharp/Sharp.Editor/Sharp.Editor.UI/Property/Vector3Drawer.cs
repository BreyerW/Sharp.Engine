using Squid;
using System;
using System.Numerics;

namespace Sharp.Editor.UI.Property
{
	public class Vector3Drawer : PropertyDrawer<Vector3>
	{
		private FlowLayoutFrame layout = new FlowLayoutFrame();
		private TextBox posX;
		private TextBox posY;
		private TextBox posZ;

		public Vector3Drawer(string name)
			: base(name)
		{
			layout.FlowDirection = FlowDirection.LeftToRight;
			layout.Position = new Point(label.Size.x, 0);
			layout.HSpacing = 0;
			layout.AutoSize = AutoSize.HorizontalVertical;
			//AutoSize = AutoSize.HorizontalVertical;
			//layout.Scissor = false;

			posX = new TextBox();
			posX.Mode = TextBoxMode.Numeric;
			var tmpLabel = new Label();
			tmpLabel.Text = "X";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posX);

			posY = new TextBox();
			posY.Mode = TextBoxMode.Numeric;
			tmpLabel = new Label();
			tmpLabel.Text = "Y";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posY);

			posZ = new TextBox();
			posZ.Mode = TextBoxMode.Numeric;
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
				var xIsEmpty = !float.TryParse(posX.Text, out var x);
				var yIsEmpty = !float.TryParse(posY.Text, out var y);
				var zIsEmpty = !float.TryParse(posZ.Text, out var z);
				return new Vector3(xIsEmpty ? 0 : x, yIsEmpty ? 0 : y, zIsEmpty ? 0 : z);
			}
			set
			{
				posX.Text = Math.Round(value.X, Application.roundingPrecision).ToString();
				posY.Text = Math.Round(value.Y, Application.roundingPrecision).ToString();
				posZ.Text = Math.Round(value.Z, Application.roundingPrecision).ToString();
				/* posX.Text = value.X.ToString("0." + new string('#', Application.roundingPrecision));
                 posY.Text = value.Y.ToString("0." + new string('#', Application.roundingPrecision));//"F"
                 posZ.Text = value.Z.ToString("0." + new string('#', Application.roundingPrecision));*/
			}
		}
	}
}