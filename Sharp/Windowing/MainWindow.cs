using System;
using System.Linq;
using SDL2;
using Sharp.Editor.Views;
using OpenTK;
using SharpSL.BackendRenderers;
using Sharp.Windowing;

namespace Sharp
{
    public class MainWindow : Window
    {
        internal static IBackendRenderer backendRenderer;
        public static Vector2 lastPos;
        public static int startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.Millisecond;

        public MainWindow(string title) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE)
        {
            SDL.SDL_SetWindowMinimumSize(handle, 500, 300);
        }

        public void Initialize(params View[] viewsToOpen)
        {
            backendRenderer.SetupGraphic();
            View.mainViews[windowId].Initialize();
            int id = 0;
            foreach (var view in viewsToOpen)
            {
                OpenView(view, id);
                ++id;
            }
        }

        public override void OnRenderFrame()
        {
        }

        public override void OnFocus()
        {
        }
    }
}