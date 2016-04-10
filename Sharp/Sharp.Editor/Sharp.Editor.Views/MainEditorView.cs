using System.Collections.Generic;
using Gwen.Control;
using Gwen;
using SharpSL;

namespace Sharp.Editor.Views
{
	public class MainEditorView:View
	{
		public static Gwen.Renderer.OpenTK renderer;
		public static Gwen.Skin.Base skin;
		public static Canvas canvas;
		public MultiVerticalSplitter splitter;
		public bool needRedraw=false;

		public static IEditorBackendRenderer editorBackendRenderer;

		public MainEditorView ()
		{
		}
		public override void Initialize ()
		{
			renderer = new Gwen.Renderer.OpenTK();
			skin = new Gwen.Skin.TexturedBase(renderer,@"C:\\Users\\Damian\\Downloads\\GLWidget_ 1\\GLWidget\\GLWidgetTest\\bin\\Debug\\DefaultSkin.png");
			canvas = new Canvas(skin);
			base.Initialize ();

			splitter = new Gwen.Control.MultiVerticalSplitter(canvas);
			splitter.SetPosition(0, 0);
			splitter.SplitterSize = 3;
			splitter.MinimumSize = new System.Drawing.Point (100,100);
			splitter.Dock = Pos.Fill;
		}
		public override void Render ()
		{
			SceneView.backendRenderer.SetupGraphic ();
			base.Render ();
			SceneView.backendRenderer.ClearBuffer ();
			canvas.RenderCanvas ();
		}
	}
}

