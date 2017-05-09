using System;
using SDL2;
using Sharp.Editor.Views;
using OpenTK;
using SharpSL.BackendRenderers;

namespace Sharp
{
    public class MainWindow : Window
    {
        internal static IBackendRenderer backendRenderer;
        public static Vector2 lastPos;
        public static int startTime = System.Diagnostics.Process.GetCurrentProcess().StartTime.Millisecond;

        public MainWindow(string title) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE)
        {
        }

        public void Initialize(params View[] viewsToOpen)
        {
            MainEditorView.editorBackendRenderer = new SharpSL.BackendRenderers.OpenGL.EditorOpenGLRenderer();
            backendRenderer = new SharpSL.BackendRenderers.OpenGL.OpenGLRenderer();
            mainView.Initialize();
            mainView.OnResize(1000, 700);

            /*mainEditorView.splitter.OnSplitMoved += (o, args) =>
            {
                var (width, height) = windowSize;
                //scene.OnResize(width, height);
                //assets.OnResize(width, height);
            };*/
            int id = 0;
            foreach (var view in viewsToOpen)
            {
                OpenView(view, this, id);
                ++id;
            }

            foreach (var view in View.views[windowId])
                view.OnContextCreated(1000, 700);
        }

        public override void OnRenderFrame()
        {
            mainView.Render();
            foreach (var view in View.views[windowId])
                if (view.panel != null && view.panel.IsVisible)
                    view.Render();
        }
    }
}