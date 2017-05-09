using System;
using SDL2;

namespace Sharp
{
    internal class FloatingWindow : Window
    {
        public FloatingWindow(string title) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE | SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS)
        {
        }

        public override void OnRenderFrame()
        {
            SDL.SDL_GL_MakeCurrent(handle, context);
            SDL.SDL_GL_SwapWindow(handle);
        }
    }
}