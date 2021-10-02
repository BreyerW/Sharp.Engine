using Sharp.Editor.Views;
using Squid;
using System;
using System.Collections.Generic;

namespace Sharp.Editor
{
    public class NativeWindow : Squid.Window
    {
        public Window win = new TooltipWindow();

        public override ControlCollection Controls
        {
            get => MainEditorView.mainViews[win.windowId].desktop.Controls; set
            {
                foreach (var child in value)
                {
                    MainEditorView.mainViews[win.windowId].desktop.Controls.Add(child);
                }
            }
        }

        public NativeWindow()
        {
            win.Hide();
            //NoEvents = true;
            VisibilityChanged += NativeWindow_VisibilityChanged;
        }

        private void NativeWindow_VisibilityChanged(Control sender)
        {
            if (sender.IsVisible)
            {
                // NoEvents = false;
                if (sender.Tag is NativeWindow natWin)
                {
                    //natWin.Update += NatWin_Update;
                    win.Position = (sender.Location.x + natWin.win.Position.x, sender.Location.y + natWin.win.Position.y);
                }
                else
                    win.Position = (sender.Location.x + sender.Canvas.screenPos.x, sender.Location.y + sender.Canvas.screenPos.y);
                //MainEditorView.mainViews[win.windowId].desktop.Update();
                var control = MainEditorView.mainViews[win.windowId].desktop.Controls[0];
                win.Size = (control.Size.x, control.Size.y);
                win.Show();
            }
            else
            {
                win.Hide();
                // NoEvents = true;
                Console.WriteLine("state changed");
            }
        }

        protected override void Draw()
        {
        }

        public override void Open()
        {
        }

        public override void Close()
        {
        }
    }
}