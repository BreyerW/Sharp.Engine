﻿using System;

namespace Gwen.Control
{
    /// <summary>
    /// Label for PropertyRow.
    /// </summary>
    public class PropertyRowLabel<T> : Label
    {
        private readonly PropertyRow<T> m_PropertyRow;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRowLabel"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public PropertyRowLabel(PropertyRow<T> parent) : base(parent)
        {
            AutoSizeToContents = false;
            Alignment = Pos.Left | Pos.CenterV;
            m_PropertyRow = parent;
        }

        /// <summary>
        /// Updates control colors.
        /// </summary>
        public override void UpdateColors()
        {
            if (IsDisabled)
            {
                TextColor = Skin.Colors.Button.Disabled;
                return;
            }

            if (m_PropertyRow != null && m_PropertyRow.IsEditing)
            {
                TextColor = Skin.Colors.Properties.Label_Selected;
                return;
            }

            if (m_PropertyRow != null && m_PropertyRow.IsHovered)
            {
                TextColor = Skin.Colors.Properties.Label_Hover;
                return;
            }

            TextColor = Skin.Colors.Properties.Label_Normal;
        }
    }
}