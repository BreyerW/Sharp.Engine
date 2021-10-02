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
            posX = new FloatField(() => Value.X, (x) => Value.X = x);//SerializedObject[nameof(Vector3.X)]
            posX.precision = Application.roundingPrecision;
            var tmpLabel = new Label();
            tmpLabel.Text = "X";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posX);

            posY = new FloatField(() => Value.Y, (y) => Value.Y = y);//SerializedObject[nameof(Vector3.Y)]
            posY.precision = Application.roundingPrecision;
            tmpLabel = new Label();
            tmpLabel.Text = "Y";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posY);

            posZ = new FloatField(() => Value.Z, (z) => Value.Z = z);//SerializedObject[nameof(Vector3.Z)]
            posZ.precision = Application.roundingPrecision;
            tmpLabel = new Label();
            tmpLabel.Text = "Z";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posZ);

            Childs.Add(layout);
        }
    }
}