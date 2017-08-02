using System;
using SharpSL;
using Squid;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
    public class MainEditorView
    {
        public static Dictionary<uint, MainEditorView> mainViews = new Dictionary<uint, MainEditorView>();
        public static MainEditorView currentMainView;
        public Desktop desktop;
        public Camera camera = new Camera();

        //public SplitContainer splitter = new SplitContainer();
        public bool needRedraw = false;

        public static IEditorBackendRenderer editorBackendRenderer;

        static MainEditorView()
        {
            Desktop.CursorSet.Add(CursorNames.Default, new NativeCursor(CursorNames.Default));
            Desktop.CursorSet.Add(CursorNames.Link, new NativeCursor(CursorNames.Link));
            Desktop.CursorSet.Add(CursorNames.Move, new NativeCursor(CursorNames.Move));
            Desktop.CursorSet.Add(CursorNames.HSplit, new NativeCursor(CursorNames.HSplit));
            Desktop.CursorSet.Add(CursorNames.VSplit, new NativeCursor(CursorNames.VSplit));
            Desktop.CursorSet.Add(CursorNames.SizeNS, new NativeCursor(CursorNames.SizeNS));
            Desktop.CursorSet.Add(CursorNames.SizeWE, new NativeCursor(CursorNames.SizeWE));
            Desktop.CursorSet.Add(CursorNames.SizeNWSE, new NativeCursor(CursorNames.SizeNWSE));
            Desktop.CursorSet.Add(CursorNames.SizeNESW, new NativeCursor(CursorNames.SizeNESW));
            Desktop.CursorSet.Add(CursorNames.Select, new NativeCursor(CursorNames.Select));
            Desktop.CursorSet.Add(CursorNames.Reject, new NativeCursor(CursorNames.Reject));
            Desktop.CursorSet.Add(CursorNames.Wait, new NativeCursor(CursorNames.Wait));
        }

        public MainEditorView(uint attachToWindow)
        {
            mainViews.Add(attachToWindow, this);
            desktop = new Desktop();
            desktop.ShowCursor = true;
            desktop.Skin = Squid.UI.GenerateStandardSkin();
            Squid.UI.Renderer = new SharpSL.BackendRenderers.UIRenderer();
        }

        //split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
        public int nextUpdate;

        public void Render()
        {
            currentMainView = this;
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);
            MainWindow.backendRenderer.Clip(0, 0, desktop.Size.x, desktop.Size.y);
            MainWindow.backendRenderer.ClearBuffer();
            MainWindow.backendRenderer.ClearColor();
            //desktop.NoEvents = Window.UnderMouseWindowId == attachedToWindow;
            OnInternalUpdate();
            desktop.Draw();
        }

        private void OnInternalUpdate()
        {
            //desktop.Update();
        }

        public void OnResize(int width, int height)
        {
            camera.SetOrthoMatrix(width, height);
            //desktop.Size = new Point(width, height);
            desktop.ResizeTo(new Point(width, height), AnchorStyles.Right | AnchorStyles.Bottom);
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);
            MainWindow.backendRenderer.Clip(0, 0, desktop.Size.x, desktop.Size.y);
            MainWindow.backendRenderer.ClearBuffer();
            MainWindow.backendRenderer.ClearColor();
        }
    }
}