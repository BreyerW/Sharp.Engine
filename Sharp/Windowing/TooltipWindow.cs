using System;
using SDL2;

namespace Sharp
{
    public class TooltipWindow : Window
    {
        public TooltipWindow() : base(default, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS, default)
        {
        }

        public override void OnRenderFrame()
        {
            //MainWindow.backendRenderer.Scissor(0, 0, Size.width, Size.height);
            //MainWindow.backendRenderer.ClearBuffer();
        }
    }
}