using System;
using SDL2;

namespace Sharp
{
    public class FloatingWindow : Window
    {
        public FloatingWindow(string title, IntPtr existingWin = default(IntPtr)) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, existingWin)
        {
        }

        public override void OnRenderFrame()
        {
            //MainWindow.backendRenderer.Scissor(0, 0, Size.width, Size.height);
            //MainWindow.backendRenderer.ClearBuffer();
        }
    }
}