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
            //posZ.TextChanged += OnValueChanged;
            //posX.TextChanged += OnValueChanged;
            //posY.TextChanged += OnValueChanged;
        }

        public override OpenTK.Vector3 Value
        {
            get
            {
                return new OpenTK.Vector3(float.Parse(posX.Text), float.Parse(posY.Text), float.Parse(posZ.Text));
            }
            set
            {
                //Console.WriteLine("buu");
                posX.Text = value.X.ToString();
                posY.Text = value.Y.ToString();
                posZ.Text = value.Z.ToString();
            }
        }
    }
}