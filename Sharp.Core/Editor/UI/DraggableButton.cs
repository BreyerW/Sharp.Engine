using Squid;
using System;

namespace Sharp.Editor.UI
{
    public class DraggableButton : Control
    {
        private Point ClickedPos;
        private bool IsDragging;

        /// <summary>
        /// Gets or sets a value indicating whether [allow drag out].
        /// </summary>
        /// <value><c>true</c> if [allow drag out]; otherwise, <c>false</c>.</value>
        public bool ConfineToParent { get; set; } = false;

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
        public DraggableButton()
        {
            Style = "keyframe";
            Scissor = false;
            MouseDown += Window_MouseDown;
            MouseUp += Window_MouseUp;
        }

        private void Window_MouseDown(Control sender, MouseEventArgs args)
        {
            if (args.Button is 0)
                StartDrag();
        }

        private void Window_MouseUp(Control sender, MouseEventArgs args)
        {
            StopDrag();
        }

        /// <summary>
        /// Starts the drag.
        /// </summary>
        public void StartDrag()
        {
            ClickedPos = Squid.UI.MousePosition - Position;
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
            Point p = Squid.UI.MousePosition - ClickedPos;

            if (ConfineToParent)
            {
                if (p.x < Parent.Padding.Left) p.x = Parent.Padding.Left;
                if (p.y < Parent.Padding.Top) p.y = Parent.Padding.Top;
                if (p.x + Size.x > Parent.Size.x - Parent.Padding.Right) p.x = Parent.Size.x - Size.x - Parent.Padding.Right;
                if (p.y + Size.y > Parent.Size.y - Parent.Padding.Bottom) p.y = Parent.Size.y - Size.y - Parent.Padding.Bottom;
            }

            Position = p;
        }
    }
}