using System;
using Gwen.Control;
using Gwen;
using SharpSL;
using Squid;
using System.Threading.Tasks;

namespace Sharp.Editor.Views
{
    public class MainEditorView : View
    {
        public Gwen.Renderer.OpenTK renderer;
        public static Gwen.Skin.Base skin;
        public Canvas canvas;
        public Desktop desktop;
        public MultiVerticalSplitter splitter;
        public bool needRedraw = false;

        public static IEditorBackendRenderer editorBackendRenderer;

        static MainEditorView()
        {
        }

        public MainEditorView(uint attachToWindow) : base(attachToWindow)
        {
            renderer = new Gwen.Renderer.OpenTK();
            skin = new Gwen.Skin.TexturedBase(renderer, @"B:\Sharp.Engine3\Gwen\DefaultSkin.png");
            canvas = new Canvas(skin);
            desktop = new Desktop();
            //desktop.Position = new Point(0, 0);
            desktop.ShowCursor = false;
            desktop.AutoSize = AutoSize.None;
            desktop.Skin = Gui.GenerateStandardSkin();
            desktop.MouseClick += Split_MouseClick;
            Gui.Renderer = new SharpSL.BackendRenderers.UIRenderer();
        }

        private Squid.Window window1 = new Squid.Window();

        public override void Initialize()
        {
            base.Initialize();
            splitter = new MultiVerticalSplitter(canvas);
            splitter.SetPosition(0, 0);
            splitter.SplitterSize = 3;
            splitter.MinimumSize = new System.Drawing.Point(100, 100);
            splitter.Dock = Pos.Fill;

            var text = new Squid.TextArea();
            text.Parent = window1;
            text.Position = new Point(20, 20);
            text.Size = new Point(100, 50);
            text.Text = "buuu";
            text.Style = "textbox";
            text.Scissor = true;
            //button powodowal problemy z memory. moze to znow problem z render textem
            var split = new SplitContainer();
            split.SplitFrame1.MinSize = new Point(300, 100);
            split.SplitFrame2.MinSize = new Point(100, 100);
            split.Parent = desktop;
            split.Dock = DockStyle.Fill;
            split.SplitButton.MouseClick += Split_MouseClick;
            //split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
            //window1.Size = new Squid.Point(440, 340);
            window1.Dock = DockStyle.Fill;
            window1.Position = new Squid.Point(40, 100);
            window1.Parent = split.SplitFrame1;
            window1.Style = "window";
        }

        private void Split_MouseClick(Squid.Control sender, MouseEventArgs args)
        {
            Console.WriteLine("split clicked");
        }

        public int nextUpdate;

        public override void Render()
        {
            //base.Render();
            MainWindow.backendRenderer.ClearBuffer();
            //canvas.RenderCanvas();\
            OnInternalUpdate();
            desktop.Draw();
        }

        private void OnInternalUpdate()
        {
            desktop.Update();
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
                //canvas?.SetSize(width, height);
            }
            //renderer?.Resize(width, height);
            Camera.main?.SetOrthoMatrix(width, height);
            desktop.Size = new Point(width, height);
        }
    }
}