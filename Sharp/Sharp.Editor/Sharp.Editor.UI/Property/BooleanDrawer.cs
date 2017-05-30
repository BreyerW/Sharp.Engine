using System;

namespace Gwen.Control.Property
{
    /// <summary>
    /// Checkable property.
    /// </summary>
    public class BooleanDrawer : Control.Property.PropertyDrawer<bool>
    {
        protected readonly Control.CheckBox m_CheckBox;

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanDrawer"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public BooleanDrawer(Control.Base parent)
            : base(parent)
        {
            m_CheckBox = new Control.CheckBox(this);
            m_CheckBox.ShouldDrawBackground = false;
            m_CheckBox.CheckChanged += OnValueChanged;
            m_CheckBox.IsTabable = true;
            m_CheckBox.KeyboardInputEnabled = true;
            m_CheckBox.SetPosition(2, 1);

            Height = 18;
        }

        /// <summary>
        /// Property value.
        /// </summary>
        public override bool Value
        {
            get { return m_CheckBox.IsChecked; }
            set { m_CheckBox.IsChecked = value; }
        }

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public override bool IsEditing
        {
            get { return m_CheckBox.HasFocus; }
        }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get { return base.IsHovered || m_CheckBox.IsHovered; }
        }
    }
}