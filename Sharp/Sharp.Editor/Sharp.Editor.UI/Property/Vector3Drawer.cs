using System;

namespace Gwen.Control.Property
{
    public class Vector3Drawer : PropertyDrawer<OpenTK.Vector3>
    {
        private TextBoxNumeric posX;
        private TextBoxNumeric posY;
        private TextBoxNumeric posZ;

        public Vector3Drawer(Control.Base parent)
            : base(parent)
        {
            posZ = new TextBoxNumeric(this);
            posZ.Dock = Pos.Right;
            posZ.Width = 50;
            //posZ.MaximumSize=new System.Drawing.Point(70,17);

            var label = new Label(this);
            label.Text = "Z ";
            label.Dock = Pos.Right;

            posY = new TextBoxNumeric(this);
            posY.Dock = Pos.Right;
            posY.Width = 50;
            //posY.MaximumSize=new System.Drawing.Point(70,17);

            label = new Label(this);
            label.Text = "Y ";
            label.Dock = Pos.Right;

            posX = new TextBoxNumeric(this);
            posX.Dock = Pos.Right;
            posX.Width = 50;
            //posX.MaximumSize=new System.Drawing.Point(70,17);

            label = new Label(this);
            label.Text = "X ";
            label.Font.Size = 7;
            label.Dock = Pos.Right;
            posZ.TextChanged += OnValueChanged;
            posX.TextChanged += OnValueChanged;
            posY.TextChanged += OnValueChanged;
        }

        public override OpenTK.Vector3 Value
        {
            get
            {
                return new OpenTK.Vector3(posX.Value, posY.Value, posZ.Value);
            }
            set
            {
                //Console.WriteLine("buu");
                posX.SetText(value.X);
                posY.SetText(value.Y);
                posZ.SetText(value.Z);
            }
        }

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public override bool IsEditing
        {
            get { return base.HasFocus | posX.HasFocus | posY.HasFocus | posZ.HasFocus; }
        }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get { return base.IsHovered | posX.IsHovered | posY.IsHovered | posZ.IsHovered; }
        }
    }
}