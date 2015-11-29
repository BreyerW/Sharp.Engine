using System.Collections.Generic;
using Gwen.Control;
using Gwen;
using OpenTK.Graphics.OpenGL;

namespace Sharp.Editor.Views
{
	public class MainEditorView:View
	{
		public static Gwen.Renderer.OpenTK renderer;
		public static Gwen.Skin.Base skin;
		public MultiVerticalSplitter splitter;
		public bool needRedraw=false;

		public MainEditorView ()
		{
		}
		public override void Initialize ()
		{
			renderer = new Gwen.Renderer.OpenTK();
			skin = new Gwen.Skin.TexturedBase(renderer,@"C:\\Users\\Damian\\Downloads\\GLWidget_ 1\\GLWidget\\GLWidgetTest\\bin\\Debug\\DefaultSkin.png");

			base.Initialize ();

			splitter = new Gwen.Control.MultiVerticalSplitter(canvas);
			splitter.SetPosition(0, 0);
			splitter.SplitterSize = 3;
			splitter.MinimumSize = new System.Drawing.Point (100,100);
			splitter.Dock = Pos.Fill;
		}
		public override void Render ()
		{
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
			base.Render ();
			GL.Clear(ClearBufferMask.DepthBufferBit|ClearBufferMask.ColorBufferBit);
			canvas.RenderCanvas ();
		}
	}
}

