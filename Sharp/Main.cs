using System;
using System.Globalization;
using SDL2;
using System.Threading;
using Sharp.Editor.Views;
using SharpAsset.Pipeline;
using System.Numerics;

namespace Sharp
{
    internal class MainClass
    {
        public static void Main(string[] args)
        {
            MainEditorView.editorBackendRenderer = new SharpSL.BackendRenderers.OpenGL.EditorOpenGLRenderer();
            MainWindow.backendRenderer = new SharpSL.BackendRenderers.OpenGL.OpenGLRenderer();
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Pipeline.Initialize();
            Pipeline.GetPipeline<FontPipeline>().Import(@"C:\Windows\Fonts\tahoma.ttf");
            // OpenTK.Graphics.GraphicsContext.ShareContexts = false;
            SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
            //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 1);
            //SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL.SDL_GLcontext.);
            MainWindow.backendRenderer.Start();

            var dummy = SDL.SDL_CreateWindow("", 0, 0, 1, 1, SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL); //convert dummy to splash screen?
                                                                                                                                             //
            SDL.SDL_GL_CreateContext(dummy);
            var id = MainWindow.backendRenderer.CreateContext(SDL.SDL_GL_GetProcAddress, SDL.SDL_GL_GetCurrentContext);
            MainWindow.contexts.Add(id);
            MainWindow.backendRenderer.MakeCurrent += SDL.SDL_GL_MakeCurrent;
            MainWindow.backendRenderer.SwapBuffers += SDL.SDL_GL_SwapWindow;

            var e = new Entity();
            e.Rotation = new Vector3((float)Math.PI, 0f, 0f);
            var cam = e.AddComponent<Camera>();
            cam.SetModelviewMatrix();
            cam.SetProjectionMatrix();
            e.name = "Main Camera";
            e.OnTransformChanged += ((sender, evnt) => cam.SetModelviewMatrix());
            Camera.main = cam;
            var mWin = new MainWindow("test"); //Console.WriteLine("alpha: " + graphic.GraphicsMode.ColorFormat.Alpha);
            mWin.Initialize(new AssetsView(mWin.windowId), new SceneView(mWin.windowId), new SceneStructureView(mWin.windowId), new InspectorView(mWin.windowId));
            SDL.SDL_DestroyWindow(dummy);
            //new FloatingWindow("", handle.t);
            var mWin2 = new MainWindow("test2");
            mWin2.Initialize(new AssetsView(mWin2.windowId));

            MainWindow.backendRenderer.EnableScissor();
            Window.PollWindows();
            SDL.SDL_Quit();
        }
    }
}