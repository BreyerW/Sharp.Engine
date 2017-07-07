using System;
using SharpSL;
using Squid;
using System.Threading.Tasks;

namespace Sharp.Editor.Views
{
    public class MainEditorView : View
    {
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
            desktop.Skin = Gui.GenerateStandardSkin();
            Gui.Renderer = new SharpSL.BackendRenderers.UIRenderer();
        }

        public override void Initialize()
        {
            base.Initialize();
            //button powodowal problemy z memory. moze to znow problem z render textem

            //split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
        }

        public int nextUpdate;

        public override void Render()
        {
            //base.Render();
            MainWindow.backendRenderer.ClearBuffer();
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
            desktop.Size = new Point(width, height);
        }
    }
}