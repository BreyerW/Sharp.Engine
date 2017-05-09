using System;
using System.Globalization;
using SDL2;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using Sharp.Editor.Views;

namespace Sharp
{
    internal class MainClass
    {
        public static void Main(string[] args)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            // OpenTK.Graphics.GraphicsContext.ShareContexts = false;
            dynamic hack = "";
            hack.ToString(); //TODO: convert dirty hack to proper preload of System.Dynamic.dll
            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");

            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            var mWin = new MainWindow("test");

            Window.context = SDL.SDL_GL_CreateContext(mWin.handle);

            OpenTK.Toolkit.Init();
            var graphic = new GraphicsContext(ContextHandle.Zero, SDL.SDL_GL_GetProcAddress,
                                                                    () => { return new ContextHandle(Window.context); });
            Console.WriteLine("alpha: " + graphic.GraphicsMode.ColorFormat.Alpha);
            mWin.Initialize(new AssetsView(mWin.windowId), new SceneView(mWin.windowId), new SceneStructureView(mWin.windowId), new InspectorView(mWin.windowId));

            var mWin2 = new MainWindow("test2");
            mWin2.Initialize(new AssetsView(mWin2.windowId));

            Window.PollWindows();
        }
    }
}