using System;
using Sharp.Commands;

namespace Gwen.Control
{
    /// <summary>
    /// Single property row.
    /// </summary>
    public class PropertyRow<T> : Base
    {
        private readonly Label m_Label;
        private readonly Property.PropertyDrawer<T> m_Property;
        private bool m_LastEditing;
        private bool m_LastHover;

        /// <summary>
        /// Invoked when the property value has changed.
        /// </summary>
        public event GwenEventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Indicates whether the property value is being edited.
        /// </summary>
        public bool IsEditing { get { return m_Property != null && m_Property.IsEditing; } }

        /// <summary>
        /// Property value.
        /// </summary>
        public T Value
        {
            get { return m_Property.Value; }
            set
            {
                //Console.WriteLine("buuu");
                m_Property.Value = value;
            }
        }

        /// <summary>
        /// Indicates whether the control is hovered by mouse pointer.
        /// </summary>
        public override bool IsHovered
        {
            get
            {
                return base.IsHovered || (m_Property != null && m_Property.IsHovered);
            }
        }

        /// <summary>
        /// Property name.
        /// </summary>
        public string Label { get { return m_Label.Text; } set { m_Label.Text = value; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRow"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        /// <param name="prop">Property control associated with this row.</param>
        public PropertyRow(Base parent, Property.PropertyDrawer<T> prop)
            : base(parent)
        {
            PropertyRowLabel<T> label = new PropertyRowLabel<T>(this);
            label.Dock = Pos.Left;
            label.Alignment = Pos.Left | Pos.Top;
            label.Margin = new Margin(2, 2, 0, 0);
            m_Label = label;

            m_Property = prop;
            m_Property.Parent = this;
            m_Property.Dock = Pos.Fill;
        }

        /// <summary>
        /// Renders the control using specified skin.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Render(Skin.Base skin)
        {
            /* SORRY */
            if (IsEditing != m_LastEditing)
            {
                OnEditingChanged();
                m_LastEditing = IsEditing;
            }

            if (IsHovered != m_LastHover)
            {
                OnHoverChanged();
                m_LastHover = IsHovered;
            }
            /* SORRY */

            skin.DrawPropertyRow(this, m_Label.Right, IsEditing, IsHovered | m_Property.IsHovered);
        }

        public override void DoRender(Gwen.Skin.Base skin)
        {
            base.DoRender(skin);
            if (isDirty)
                Value = m_Property.getter();
        }

        /// <summary>
        /// Lays out the control's interior according to alignment, padding, dock etc.
        /// </summary>
        /// <param name="skin">Skin to use.</param>
        protected override void Layout(Skin.Base skin)
        {
            Sharp.Control.Properties parent = Parent as Sharp.Control.Properties;
            if (null == parent) return;

            m_Label.Width = 70;

            if (m_Property != null)
            {
                Height = m_Property.Height;
            }
        }

        protected virtual void OnValueChanged(Base control, EventArgs args)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnEditingChanged()
        {
            m_Label.Redraw();
        }

        private void OnHoverChanged()
        {
            m_Label.Redraw();
        }
    }
}