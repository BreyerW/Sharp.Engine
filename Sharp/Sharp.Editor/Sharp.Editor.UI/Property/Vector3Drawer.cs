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
            posZ.TextChanged += OnValueChanged;
            var label = new Label(this);
            label.Text = "Z ";
            label.Dock = Pos.Right;

            posY = new TextBoxNumeric(this);
            posY.Dock = Pos.Right;
            posY.Width = 50;
            //posY.MaximumSize=new System.Drawing.Point(70,17);
            posY.TextChanged += OnValueChanged;
            label = new Label(this);
            label.Text = "Y ";
            label.Dock = Pos.Right;

            posX = new TextBoxNumeric(this);
            posX.Dock = Pos.Right;
            posX.Width = 50;
            //posX.MaximumSize=new System.Drawing.Point(70,17);
            posX.TextChanged += OnValueChanged;
            label = new Label(this);
            label.Text = "X ";
            label.Font.Size = 7;
            label.Dock = Pos.Right;
        }

        public override OpenTK.Vector3 Value
        {
            get
            {
                return new OpenTK.Vector3(posX.Value, posY.Value, posZ.Value);
            }
            set
            {
                base.Value = value;
            }
        }

        /// <summary>
        /// Sets the property value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        /// <param name="fireEvents">Determines whether to fire "value changed" event.</param>
        public override void SetValue(OpenTK.Vector3 value, bool fireEvents = false)
        {
            var val = value;
            var Val = Value;
            posX.SetText(val.X, val.X != Val.X);
            posY.SetText(val.Y, val.Y != Val.Y);
            posZ.SetText(val.Z, val.Y != Val.Z);
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