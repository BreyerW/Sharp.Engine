using System;
using Gwen.Control;

namespace Sharp.Editor.Views
{
	public class SceneStructureView:View
	{
		public static TreeControl tree;

		public override void Initialize ()
		{
			base.Initialize ();
			if(tree==null)
			tree = new  Gwen.Control.TreeControl (canvas);
			tree.SetSize (canvas.Width, canvas.Height);
			tree.ShouldDrawBackground = false;
		}
		public static void RegisterEntity(Entity ent){
			var id=SceneView.entities.IndexOf (ent);
			tree.AddNode (()=>SceneView.entities[id]);
		}
		public override void OnMouseMove (OpenTK.Input.MouseMoveEventArgs evnt)
		{
			base.OnMouseMove (evnt);
		}
	}
}

