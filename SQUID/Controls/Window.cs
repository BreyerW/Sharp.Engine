using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace Squid
{
    /// <summary>
    /// A Window
    /// </summary>
    [Toolbox]
    public class Window : Control, IControlContainer
    {
        private Resizer Sizer;
        private Point ClickedPos;
        private bool IsDragging;

        [Hidden]
        public virtual ControlCollection Controls { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Window"/> is modal.
        /// </summary>
        /// <value><c>true</c> if modal; otherwise, <c>false</c>.</value>
        [Hidden]
        public bool Modal { get; set; }

        /// <summary>
        /// Gets or sets the snap distance.
        /// </summary>
        /// <value>The snap distance.</value>
        [Category("Behavior")]
        public int SnapDistance { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [allow drag out].
        /// </summary>
        /// <value><c>true</c> if [allow drag out]; otherwise, <c>false</c>.</value>
        [Category("Behavior")]
        public bool ConfineToParent { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Window"/> is resizable.
        /// </summary>
        /// <value><c>true</c> if resizable; otherwise, <c>false</c>.</value>
        [Category("Behavior")]
        public bool Resizable
        {
            get { return Sizer.ParentControl == this; }
            set
            {
                if (value)
                {
                    if (Sizer.ParentControl != this)
                        Childs.Add(Sizer);
                }
                else
                {
                    Childs.Remove(Sizer);
                }
            }
        }

        /// <summary>
        /// Gets or sets the size of the grip.
        /// </summary>
        /// <value>The size of the grip.</value>
        [Category("Behavior")]
        public Margin GripSize
        {
            get { return Sizer.GripSize; }
            set { Sizer.GripSize = value; }
        }

        /// <summary>
        /// Called when [update].
        /// </summary>
        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (IsDragging)
                Drag();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Window"/> class.
        /// </summary>
        public Window()
        {
            Style = "window";
            Scissor = true;
            Sizer = new Resizer();
            Sizer.Dock = DockStyle.Fill;
            SnapDistance = 12;

            MinSize = new Point(200, 100);
            MaxSize = new Point(800, 600);
        }

        protected override void DrawBefore()
        {
            if (Modal)
            {
                SetScissor(0, 0, Desktop.Size.x, Desktop.Size.y);
                UI.Renderer.DrawBox(0, 0, Desktop.Size.x, Desktop.Size.y, ColorInt.FromArgb(GetOpacity(1), Desktop.ModalColor));
                ResetScissor();
            }

            base.DrawBefore();
        }

        /// <summary>
        /// Starts the drag.
        /// </summary>
        public void StartDrag()
        {
            ClickedPos = UI.MousePosition - Position;
            IsDragging = true;
        }

        /// <summary>
        /// Stops the drag.
        /// </summary>
        public void StopDrag()
        {
            IsDragging = false;
        }

        private void Drag()
        {
            Point p = UI.MousePosition - ClickedPos;

            if (!Modal)
            {
                foreach (Control win in Container.Controls)
                {
                    if (!(win is Window)) continue;
                    if (win == this) continue;

                    int top = win.Position.y;
                    int bottom = win.Position.y + win.Size.y;
                    int left = win.Position.x;
                    int right = win.Position.x + win.Size.x;

                    if (Math.Abs(p.x - right) <= SnapDistance)
                    {
                        if (!(p.y + Size.y < top) && !(p.y > bottom))
                            p.x = right;
                    }

                    if (Math.Abs(p.x + Size.x - left) <= SnapDistance)
                    {
                        if (!(p.y + Size.y < top) && !(p.y > bottom))
                            p.x = left - Size.x;
                    }

                    if (Math.Abs(p.y - bottom) <= SnapDistance)
                    {
                        if (!(p.x + Size.x < left) && !(p.x > right))
                            p.y = bottom;
                    }

                    if (Math.Abs(p.y + Size.y - top) <= SnapDistance)
                    {
                        if (!(p.x + Size.x < left) && !(p.x > right))
                            p.y = top - Size.y;
                    }
                }
            }

            if (ConfineToParent)
            {
                if (p.x < 0) p.x = 0;
                if (p.y < 0) p.y = 0;
                if (p.x + Size.x > Parent.Size.x) p.x = Parent.Size.x - Size.x;
                if (p.y + Size.y > Parent.Size.y) p.y = Parent.Size.y - Size.y;

                if (p.x < SnapDistance) p.x = 0;
                if (p.y < SnapDistance) p.y = 0;
                if (p.x + Size.x > Parent.Size.x - SnapDistance) p.x = Parent.Size.x - Size.x;
                if (p.y + Size.y > Parent.Size.y - SnapDistance) p.y = Parent.Size.y - Size.y;
            }

            Position = p;
        }

        /// <summary>
        /// Shows this window on the specified Desktop.
        /// </summary>
        /// <param name="target">The target.</param>
        public virtual void Open()
        {
            SetDepth();

            if (Modal)
                Desktop.RegisterModal(this);

            IsVisible = true;
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        public virtual void Close()
        {
            Console.WriteLine("close");
            if (Desktop == null) return;

            if (Modal)
                Desktop.UnregisterModal(this);

            IsVisible = false;
        }
    }
}