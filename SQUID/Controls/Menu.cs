using System;
using System.Collections.Generic;
using System.Text;

namespace Squid
{
    /// <summary>
    /// A DropDownButton
    /// </summary>
    public class Menu : Button
    {
        /// <summary>
        /// Gets the dropdown.
        /// </summary>
        /// <value>The dropdown.</value>
        public Window Dropdown { get; private set; }

        /// <summary>
        /// Gets the Layout frame.
        /// </summary>
        /// <value>The dropdown.</value>
        public FlowLayoutFrame Frame { get; private set; }

        /// <summary>
        /// Gets or sets the align.
        /// </summary>
        /// <value>The align.</value>
        public Alignment Align { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [hot drop].
        /// </summary>
        /// <value><c>true</c> if [hot drop]; otherwise, <c>false</c>.</value>
        public bool HotDrop { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is open.
        /// </summary>
        /// <value><c>true</c> if this instance is open; otherwise, <c>false</c>.</value>
        public bool IsOpen { get; private set; }

        /// <summary>
        /// Raised when [on closed].
        /// </summary>
        public event EventWithArgs OnClosed;

        /// <summary>
        /// Raised when [on opened].
        /// </summary>
        public event EventWithArgs OnOpened;

        /// <summary>
        /// Raised when [on opening].
        /// </summary>
        public event EventWithArgs OnOpening;

        /// <summary>
        /// Raised when [on closing].
        /// </summary>
        public event EventWithArgs OnClosing;

        /// <summary>
        /// Initializes a new instance of the <see cref="DropDownButton"/> class.
        /// </summary>
        public Menu()
        {
            Dropdown = new Window();
            Dropdown.Scissor = false;
            Dropdown.Style = "";
            Dropdown.AutoSize = AutoSize.HorizontalVertical;
            Align = Alignment.BottomLeft;

            Frame = new FlowLayoutFrame();
            Frame.Style = "";
            Frame.AutoSize = AutoSize.HorizontalVertical;
            Frame.FlowDirection = FlowDirection.TopToBottom;
            Frame.VSpacing = -1;
            Dropdown.Controls.Add(Frame);
            Frame.Controls.BeforeItemAdded += Childs_BeforeItemAdded;

            MouseClick += Button_MouseClick;
            MouseDown += Button_MouseDown;
            MouseEnter += Button_MouseEnter;
        }

        /// <summary>
        /// Opens this instance.
        /// </summary>
        public void Open()
        {
            if (OnOpening != null)
            {
                SquidEventArgs args = new SquidEventArgs();
                OnOpening(this, args);
                if (args.Cancel) return;
            }

            if (HotDrop && Dropdown.Controls.Count == 0) return;

            Dropdown.Owner = Parent;

            switch (Align)
            {
                case Alignment.BottomLeft:
                    Dropdown.Position = Location + new Point(0, Size.y);
                    break;

                case Alignment.TopRight:
                    Dropdown.Position = Location + new Point(Size.x, 0);
                    break;

                case Alignment.TopLeft:
                    Dropdown.Position = Location - new Point(Dropdown.Size.x, 0);
                    break;
            }

            Desktop.ShowDropdown(Dropdown, true);
            IsOpen = true;

            if (OnOpened != null)
                OnOpened(this, null);
        }

        public Button AddItem(string menuOption)
        {
            var arr = menuOption.Split('/');
            var id = 0;
            var container = Frame;
            foreach (var option in arr)
            {
                if (arr.Length > 1 && id < arr.Length - 1)
                {
                    var dropdown = Frame.GetControl(option) as Menu;
                    if (dropdown == null)
                    {
                        dropdown = new Menu();
                        dropdown.HotDrop = true;
                        dropdown.Text = option;
                        dropdown.Name = option;
                        dropdown.Dropdown.Style = "";
                        dropdown.Align = Align;
                        dropdown.Frame.Controls.BeforeItemAdded += Childs_BeforeItemAdded;
                        container.Controls.Add(dropdown);
                    }
                    container = dropdown.Frame;
                }
                id++;
            }
            var button = new Button();
            button.Text = arr[arr.Length - 1];
            button.TextAlign = Alignment.MiddleLeft;
            container.Controls.Add(button);
            return button;
        }

        private void Childs_BeforeItemAdded(object sender, ListEventArgs<Control> e)
        {
            e.Item.MouseClick += Item_MouseClick;
        }

        private void Item_MouseClick(Control sender, MouseEventArgs args)
        {
            Close();
        }

        public void Close()
        {
            if (Desktop == null) return;

            if (OnClosing != null)
            {
                SquidEventArgs args = new SquidEventArgs();
                OnClosing(this, args);
                if (args.Cancel) return;
            }

            Desktop.CloseDropdowns();
            IsOpen = false;

            if (OnClosed != null)
                OnClosed(this, null);
        }

        public override bool Contains(Control control)
        {
            if (control.IsChildOf(this))
                return true;

            return control.IsChildOf(Dropdown);
        }

        private void Button_MouseEnter(Control sender)
        {
            if (HotDrop) Open();
        }

        private void Button_MouseDown(Control sender, MouseEventArgs args)
        {
            if (Dropdown.Parent == null) IsOpen = false;
        }

        private void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            if (IsOpen)
                Close();
            else
                Open();
        }
    }
}