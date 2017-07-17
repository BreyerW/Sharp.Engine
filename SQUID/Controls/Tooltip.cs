using System;
using System.Collections.Generic;
using System.Text;

namespace Squid
{
    /// <summary>
    /// The Tooltip base class. Inherit this to create custom Tooltip controls.
    /// </summary>
    public abstract class Tooltip : Frame
    {
        /// <summary>
        /// Gets or sets the offset.
        /// </summary>
        /// <value>The offset.</value>
        public Point Offset { get; set; }

        /// <summary>
        /// Gets or sets the alignment.
        /// </summary>
        /// <value>The alignment.</value>
        public Alignment Alignment { get; set; }

        ///// <summary>
        ///// Gets or sets a value indicating whether [auto layout].
        ///// </summary>
        ///// <value><c>true</c> if [auto layout]; otherwise, <c>false</c>.</value>
        //public bool AutoLayout { get; set; }

        public Tooltip()
        {
            //AutoLayout = true;
            Alignment = Alignment.BottomRight;
            Offset = new Point(-8, -8);
        }

        /// <summary>
        /// Sets the context.
        /// </summary>
        /// <param name="context">The context.</param>
        public abstract void SetContext(Control context);

        public virtual void LayoutTooltip()
        {
            Point p = UI.MousePosition;

            switch (Alignment)
            {
                case Alignment.TopLeft:
                    p = UI.MousePosition;
                    break;
                case Alignment.TopRight:
                    p = UI.MousePosition - new Point(Size.x, 0);
                    break;
                case Alignment.TopCenter:
                    p = UI.MousePosition - new Point(Size.x / 2, 0);
                    break;
                case Alignment.MiddleLeft:
                    p = UI.MousePosition - new Point(0, Size.y / 2);
                    break;
                case Alignment.MiddleRight:
                    p = UI.MousePosition - new Point(Size.x, Size.y / 2);
                    break;
                case Alignment.MiddleCenter:
                    p = UI.MousePosition - new Point(Size.x / 2, Size.y / 2);
                    break;
                case Alignment.BottomRight:
                    p = UI.MousePosition - new Point(Size.x, Size.y);
                    break;
                case Alignment.BottomLeft:
                    p = UI.MousePosition - new Point(0, Size.y);
                    break;
                case Alignment.BottomCenter:
                    p = UI.MousePosition - new Point(Size.x / 2, Size.y);
                    break;
            }

            p += Offset;

            if (p.x < 0) p.x = 0;
            if (p.y < 0) p.y = 0;

            Point p2 = p + Size;

            if (p2.x > Desktop.Size.x)
                p.x = Desktop.Size.x - Size.x;

            if (p2.y > Desktop.Size.y)
                p.y = Desktop.Size.y - Size.y;

            Position = p;
            PerformUpdate();
        }
    }
}
