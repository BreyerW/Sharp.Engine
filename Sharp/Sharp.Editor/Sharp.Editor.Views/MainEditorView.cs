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
        }

        public MainEditorView(uint attachToWindow)
        {
            mainViews.Add(attachToWindow, this);
            desktop = new Desktop();
            desktop.ShowCursor = true;
            desktop.CursorSet.Add(CursorNames.Default, new NativeCursor(CursorNames.Default));
            desktop.CursorSet.Add(CursorNames.Link, new NativeCursor(CursorNames.Link));
            desktop.CursorSet.Add(CursorNames.Move, new NativeCursor(CursorNames.Move));
            desktop.CursorSet.Add(CursorNames.HSplit, new NativeCursor(CursorNames.HSplit));
            desktop.CursorSet.Add(CursorNames.VSplit, new NativeCursor(CursorNames.VSplit));
            desktop.CursorSet.Add(CursorNames.SizeNS, new NativeCursor(CursorNames.SizeNS));
            desktop.CursorSet.Add(CursorNames.SizeWE, new NativeCursor(CursorNames.SizeWE));
            desktop.CursorSet.Add(CursorNames.SizeNWSE, new NativeCursor(CursorNames.SizeNWSE));
            desktop.CursorSet.Add(CursorNames.SizeNESW, new NativeCursor(CursorNames.SizeNESW));
            desktop.CursorSet.Add(CursorNames.Select, new NativeCursor(CursorNames.Select));
            desktop.CursorSet.Add(CursorNames.Reject, new NativeCursor(CursorNames.Reject));
            desktop.CursorSet.Add(CursorNames.Wait, new NativeCursor(CursorNames.Wait));
            desktop.Skin = Squid.UI.GenerateStandardSkin();
            Squid.UI.Renderer = new SharpSL.BackendRenderers.UIRenderer();
        }

        //button powodowal problemy z memory. moze to znow problem z render textem

        //split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
        public int nextUpdate;

        public void Render()
        {
            currentMainView = this;
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);

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
            //desktop.Size = ;
            desktop.ResizeTo(new Point(width, height), AnchorStyles.Right | AnchorStyles.Bottom);
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);
        }
    }
}