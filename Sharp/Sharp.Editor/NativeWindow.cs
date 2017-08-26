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

        public NativeWindow()
        {
            win.Hide();
            PositionChanged += NativeWindow_PositionChanged;
            NoEvents = true;
        }

        private void NativeWindow_PositionChanged(Control sender)
        {
            // win.Position = (sender.Location.x + canvas.screenPos.x, sender.Location.y + canvas.screenPos.y);
        }

        protected override void Draw()
        {
        }

        public override void Open(Desktop target)
        {
            canvas = target;
            //target.Controls.Add(this);
            /* foreach (var child in Controls.ToArray())
             {
                 MainEditorView.mainViews[win.windowId].desktop.Controls.Add(child);
             }
             MainEditorView.mainViews[win.windowId].desktop.Parent = this.Parent;*/
            NoEvents = false;
            win.Position = (Location.x + canvas.screenPos.x, Location.y + canvas.screenPos.y);
            //NativeWindow_PositionChanged(this);
            //MainEditorView.mainViews[win.windowId].desktop.Update();
            //var control = MainEditorView.mainViews[win.windowId].desktop;
            win.Size = (Controls[0].Size.x, Controls[0].Size.y);
            win.Show();
            SDL2.SDL.SDL_RaiseWindow(win.handle);
        }

        public override void Close()
        {
            win.Hide();
            NoEvents = true;
            Console.WriteLine("state changed");
        }
    }
}