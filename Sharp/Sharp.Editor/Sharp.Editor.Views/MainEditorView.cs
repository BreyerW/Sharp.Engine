using System;
using Gwen.Control;
using Gwen;
using SharpSL;
using Squid;

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

            window1.Size = new Squid.Point(440, 340);
            window1.Position = new Squid.Point(40, 40);
            window1.Resizable = true;
            window1.Parent = desktop;

            Squid.Button button = new Squid.Button();
            button.Size = new Squid.Point(70, 50);
            button.Position = new Squid.Point(20, 20);
            button.Text = "buuu";
            button.Style = "button";
            button.Parent = window1;
            button.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            //button.Cursor = Cursors.Link;
            button.MouseClick += (sender, args) => Console.WriteLine("clicked");
            button.Scissor = true;
        }

        public override void Render()
        {
            base.Render();
            MainWindow.backendRenderer.ClearBuffer();
            //canvas.RenderCanvas();
            //Console.WriteLine("drawtexture");
            desktop.PerformLayout();
            //desktop.ProcessEvents();
            desktop.Update();
            desktop.Draw();
            MainWindow.backendRenderer.Clip(0, 0, desktop.Size.x, desktop.Size.y);
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