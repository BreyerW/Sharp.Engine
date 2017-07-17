﻿using System;
using SharpSL;
using Squid;
using System.Threading.Tasks;

namespace Sharp.Editor.Views
{
    public class MainEditorView : View
    {
        protected override string Name => "Main View";

        public Desktop desktop;

        //public SplitContainer splitter = new SplitContainer();
        public bool needRedraw = false;

        public static IEditorBackendRenderer editorBackendRenderer;

        static MainEditorView()
        {
        }

        public MainEditorView(uint attachToWindow) : base(attachToWindow)
        {
            desktop = new Desktop();
            desktop.ShowCursor = false;
            desktop.Skin = Squid.UI.GenerateStandardSkin();
            Squid.UI.Renderer = new SharpSL.BackendRenderers.UIRenderer();
        }

        //button powodowal problemy z memory. moze to znow problem z render textem

        //split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
        public int nextUpdate;

        public override void Render()
        {
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);

            MainWindow.backendRenderer.ClearBuffer();
            MainWindow.backendRenderer.ClearColor();
            OnInternalUpdate();
            desktop.Draw();
        }

        private void OnInternalUpdate()
        {
            desktop.Update();
        }

        public override void OnResize(int width, int height)
        {
            Camera.main?.SetOrthoMatrix(width, height);
            //desktop.Size = ;
            desktop.ResizeTo(new Point(width, height), AnchorStyles.Right | AnchorStyles.Bottom);
            MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);
        }
    }
}