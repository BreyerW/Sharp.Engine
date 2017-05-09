using System.Collections.Generic;
using Gwen.Control;
using Gwen;
using SharpSL;

namespace Sharp.Editor.Views
{
    public class MainEditorView : View
    {
        public Gwen.Renderer.OpenTK renderer;
        public static Gwen.Skin.Base skin;
        public Canvas canvas;
        public MultiVerticalSplitter splitter;
        public bool needRedraw = false;

        public static IEditorBackendRenderer editorBackendRenderer;

        public MainEditorView(uint attachToWindow) : base(attachToWindow)
        {
        }

        public override void Initialize()
        {
            renderer = new Gwen.Renderer.OpenTK();
            skin = new Gwen.Skin.TexturedBase(renderer, @"C:\\Users\\Damian\\Downloads\\GLWidget_ 1\\GLWidget\\GLWidgetTest\\bin\\Debug\\DefaultSkin.png");
            canvas = new Canvas(skin);
            base.Initialize();

            splitter = new Gwen.Control.MultiVerticalSplitter(canvas);
            splitter.SetPosition(0, 0);
            splitter.SplitterSize = 3;
            splitter.MinimumSize = new System.Drawing.Point(100, 100);
            splitter.Dock = Pos.Fill;
        }

        public override void Render()
        {
            MainWindow.backendRenderer.SetupGraphic();
            base.Render();
            MainWindow.backendRenderer.ClearBuffer();
            canvas.RenderCanvas();
        }

        public override void OnResize(int width, int height)
        {
            if (panel != null)
            {
                //	var absPos =MainEditorView.canvas.CanvasPosToLocal (new System.Drawing.Point (panel.X, panel.Y));
                //panel.SetSize(width, height);
            }
            else
            {
                canvas.SetSize(width, height);
            }
            renderer.Resize(width, height);
        }
    }
}