﻿using SDL2;
using System;

namespace Sharp
{
    public class TooltipWindow : Window
    {
        public TooltipWindow() : base(default, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS, default)
        {
        }

        public override void OnRenderFrame()
        {
            //PluginManager.backendRenderer.Scissor(0, 0, Size.width, Size.height);
            //PluginManager.backendRenderer.ClearBuffer();
        }
    }
}