using System;
using Squid;

namespace Sharp.Editor.UI.Property
{
    public class Vector3Drawer : PropertyDrawer<OpenTK.Vector3>
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
            layout.HSpacing = 1;
            layout.AutoSize = AutoSize.HorizontalVertical;
            //AutoSize = AutoSize.HorizontalVertical;
            //layout.Scissor = false;

            posX = new TextBox();
            //posX.MaximumSize=new System.Drawing.Point(70,17);
            var tmpLabel = new Label();
            tmpLabel.Text = "X";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posX);
            //posZ.MaximumSize=new System.Drawing.Point(70,17);

            posY = new TextBox();
            //posY.MaximumSize=new System.Drawing.Point(70,17);
            tmpLabel = new Label();
            tmpLabel.Text = "Y";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posY);

            posZ = new TextBox();
            tmpLabel = new Label();
            tmpLabel.Text = "Z";
            tmpLabel.Size = new Point(0, Size.y);
            tmpLabel.AutoSize = AutoSize.Horizontal;

            layout.Controls.Add(tmpLabel);
            layout.Controls.Add(posZ);

            Childs.Add(layout);
            posX.TextChanged += (sender) => isDirty = true;
            posY.TextChanged += (sender) => isDirty = true;
            posZ.TextChanged += (sender) => isDirty = true;
        }

        public override OpenTK.Vector3 Value
        {
            get
            {
                var xIsEmpty = !float.TryParse(posX.Text, out var x);
                var yIsEmpty = !float.TryParse(posY.Text, out var y);
                var zIsEmpty = !float.TryParse(posZ.Text, out var z);
                return new OpenTK.Vector3(xIsEmpty ? 0 : x, yIsEmpty ? 0 : y, zIsEmpty ? 0 : z);
            }
            set
            {
                posX.Text = value.X.ToString();
                posY.Text = value.Y.ToString();
                posZ.Text = value.Z.ToString();
            }
        }
    }
}