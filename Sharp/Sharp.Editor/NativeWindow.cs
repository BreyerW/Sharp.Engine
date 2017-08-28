using System;
using System.Collections.Generic;
using Squid;
using Sharp.Editor.Views;

namespace Sharp.Editor
{
    public class NativeWindow : Squid.Window
    {
        public Window win = new TooltipWindow();
        private Desktop canvas;

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
            PositionChanged += NativeWindow_PositionChanged;
            //NoEvents = true;
        }

        private void NativeWindow_PositionChanged(Control sender)
        {
            win.Position = Tag is NativeWindow natWin ? (sender.Location.x + natWin.win.Position.x, sender.Location.y + natWin.win.Position.y) : (sender.Location.x + canvas.screenPos.x, sender.Location.y + canvas.screenPos.y);
        }

        protected override void Draw()
        {
        }

        public override void Open(Desktop target)
        {
            // NoEvents = false;
            canvas = target;
            //target.Controls.Add(this);

            //NativeWindow_PositionChanged(this);
            MainEditorView.mainViews[win.windowId].desktop.Update();
            //var control = MainEditorView.mainViews[win.windowId].desktop;
            win.Size = (Controls[0].Size.x, Controls[0].Size.y);
            win.Show();
            SDL2.SDL.SDL_RaiseWindow(win.handle);
        }

        public override void Close()
        {
            win.Hide();
            // NoEvents = true;
            Console.WriteLine("state changed");
        }
    }
}