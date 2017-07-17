﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Squid
{
    /// <summary>
    /// A container with scrollbars
    /// </summary>
    [Toolbox]
    public class Panel : Control
    {
        /// <summary>
        /// Gets the content.
        /// </summary>
        /// <value>The content.</value>
        public Frame Content { get; private set; }

        /// <summary>
        /// Gets the clip frame.
        /// </summary>
        /// <value>The clip frame.</value>
        public Frame ClipFrame { get; private set; }
        
        /// <summary>
        /// Gets the H scroll.
        /// </summary>
        /// <value>The H scroll.</value>
        public ScrollBar HScroll { get; private set; }
        
        /// <summary>
        /// Gets the V scroll.
        /// </summary>
        /// <value>The V scroll.</value>
        public ScrollBar VScroll { get; private set; }

        public bool UseWheelForHScroll;

        /// <summary>
        /// Initializes a new instance of the <see cref="Panel"/> class.
        /// </summary>
        public Panel()
        {
            Size = new Point(100, 100);

            VScroll = new ScrollBar();
            VScroll.Dock = DockStyle.Right;
            VScroll.Size = new Point(25, 25);
            Childs.Add(VScroll);

            HScroll = new ScrollBar();
            HScroll.Dock = DockStyle.Bottom;
            HScroll.Size = new Point(25, 25);
            HScroll.Orientation = Orientation.Horizontal;
            Childs.Add(HScroll);

            ClipFrame = new Frame();
            ClipFrame.Dock = DockStyle.Fill;
            ClipFrame.Scissor = true;
            Childs.Add(ClipFrame);

            Content = new Frame();
            Content.AutoSize = AutoSize.Vertical;
            ClipFrame.Controls.Add(Content);

            MouseWheel += Panel_MouseWheel;
        }

        void Panel_MouseWheel(Control sender, MouseEventArgs args)
        {
            // scroll

            if (UseWheelForHScroll)
                HScroll.Scroll(UI.MouseScroll);
            else
                VScroll.Scroll(UI.MouseScroll);

            // consume the mouse event
            args.Cancel = true;
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            Point position = Point.Zero;

            if (Content.Size.x < ClipFrame.Size.x || Content.AutoSize == Squid.AutoSize.Vertical)
                Content.Size = new Point(ClipFrame.Size.x, Content.Size.y);

            if (Content.Size.y < ClipFrame.Size.y || Content.AutoSize == Squid.AutoSize.Horizontal)
                Content.Size = new Point(Content.Size.x, ClipFrame.Size.y);

            if (!VScroll.ShowAlways && Content.Size.y <= ClipFrame.Size.y)
            {
                VScroll.IsVisible = false;
            }
            else
            {
                VScroll.IsVisible = true;
                VScroll.Scale = Math.Min(1, (float)ClipFrame.Size.y / (float)Content.Size.y);
                position.y = (int)((ClipFrame.Size.y - Content.Size.y) * VScroll.EasedValue);

                //hack
                if (VScroll.ShowAlways)
                    VScroll.Slider.IsVisible = VScroll.Scale < 1;
                else
                    VScroll.Slider.IsVisible = true;
            }

            if (!HScroll.ShowAlways && Content.Size.x <= ClipFrame.Size.x)
            {
                HScroll.IsVisible = false;
            }
            else
            {
                HScroll.IsVisible = true;
                HScroll.Scale = Math.Min(1, (float)ClipFrame.Size.x / (float)Content.Size.x);
                position.x = (int)((ClipFrame.Size.x - Content.Size.x) * HScroll.EasedValue);
            }

            Content.Position = position;
        }
    }
}
