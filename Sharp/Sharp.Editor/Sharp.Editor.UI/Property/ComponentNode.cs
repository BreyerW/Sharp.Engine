using System;
using System.Collections.Generic;
using Squid;

namespace Sharp.Editor.UI.Property
{
    public class ComponentNode : TreeNode
    {
        /// <summary>
        /// Gets the button.
        /// </summary>
        /// <value>The button.</value>
        public Button Button { get; private set; }

        /// <summary>
        /// Gets the label.
        /// </summary>
        /// <value>The label.</value>
        public Label Label { get; private set; }

        public FlowLayoutFrame Frame { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TreeNodeLabel"/> class.
        /// </summary>
        public ComponentNode()
        {
            AutoSize = AutoSize.HorizontalVertical;
            //Scissor = true;
            Button = new Button();
            Button.Size = new Point(10, 10);
            Button.Margin = new Margin(3, 3, 3, 3);
            //Button.Dock = DockStyle.Top;
            Button.MouseClick += Button_MouseClick;
            Childs.Add(Button);

            Label = new Button();
            Label.Size = new Point(20, 20);
            Label.Margin = new Margin(13, 0, 0, 0);
            Label.Dock = DockStyle.FillX;
            Label.MouseClick += Label_MouseClick;
            Label.NoEvents = true;
            Label.Style = "";
            Childs.Add(Label);
            var margin = Label.Size.y;
            Frame = new FlowLayoutFrame();
            Frame.Position = new Point(10, Label.Size.y);
            //Frame.Margin = new Margin(10, 0, 0, margin);
            Frame.AutoSize = AutoSize.HorizontalVertical;
            Frame.FlowDirection = FlowDirection.TopToBottom;
            Frame.IsVisible = false;
            Frame.VSpacing = 1;
            Frame.Scissor = true;
            Childs.Add(Frame);

            MouseClick += Label_MouseClick;
        }

        //protected override void OnStateChanged()
        //{
        //    Label.State = State;
        //}

        private void Label_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            IsSelected = true;
            Frame.IsVisible = Expanded;
        }

        private void Button_MouseClick(Control sender, MouseEventArgs args)
        {
            if (args.Button > 0) return;

            Expanded = !Expanded;
            Frame.IsVisible = Expanded;
            //this.ResizeTo(new Point(Size.x, Expanded ? (Frame.Size.y + Label.Size.y) : Label.Size.y), AnchorStyles.Bottom | AnchorStyles.Right);
        }
    }
}