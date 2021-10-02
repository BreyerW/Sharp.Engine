using SDL2;
using System;

namespace Sharp
{
    public class FloatingWindow : Window
    {
        public FloatingWindow(string title, IntPtr existingWin = default(IntPtr)) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE, existingWin)
        {
        }

        public override void OnRenderFrame()
        {
            //PluginManager.backendRenderer.Scissor(0, 0, Size.width, Size.height);
            //PluginManager.backendRenderer.ClearBuffer();
        }
    }
}