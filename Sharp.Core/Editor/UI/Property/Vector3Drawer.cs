using Sharp.Engine.Components;
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

		public Vector3Drawer(MemberInfo memInfo) : base(memInfo)
		{
			layout.FlowDirection = FlowDirection.LeftToRight;
			layout.Position = new Point(label.Size.x, 0);
			layout.HSpacing = 0;
			layout.AutoSize = AutoSize.HorizontalVertical;
			//AutoSize = AutoSize.HorizontalVertical;
			//layout.Scissor = false;
			posX = new FloatField(() => ref Value.X);//SerializedObject[nameof(Vector3.X)]
			posX.precision = Application.roundingPrecision;
			posX.TextChanged += Pos_TextChanged;
			var tmpLabel = new Label();
			tmpLabel.Text = "X";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posX);

			posY = new FloatField(() => ref Value.Y);//SerializedObject[nameof(Vector3.Y)]
			posY.precision = Application.roundingPrecision;
			posY.TextChanged += Pos_TextChanged;
			tmpLabel = new Label();
			tmpLabel.Text = "Y";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posY);

			posZ = new FloatField(() => ref Value.Z);//SerializedObject[nameof(Vector3.Z)]
			posZ.precision = Application.roundingPrecision;
			posZ.TextChanged += Pos_TextChanged;
			tmpLabel = new Label();
			tmpLabel.Text = "Z";
			tmpLabel.Size = new Point(0, Size.y);
			tmpLabel.AutoSize = AutoSize.Horizontal;

			layout.Controls.Add(tmpLabel);
			layout.Controls.Add(posZ);

			Childs.Add(layout);
		}

		private void Pos_TextChanged(Control sender)
		{
			if (Target is Transform t)
				t.SetModelMatrix();
		}
	}
}