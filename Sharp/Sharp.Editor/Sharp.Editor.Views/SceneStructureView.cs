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
			tree.SelectionChanged += (sender, args) => Selection.Asset = (sender as TreeNode).Content;

			SceneView.OnAddedEntity += ReconstructTree;
			SceneView.OnRemovedEntity += ReconstructTree;
			ReconstructTree ();
		}
		private void ReconstructTree(){
			tree.RemoveAll ();
			RegisterEntity(Camera.main.entityObject);
			foreach (var entity in SceneView.entities)
				RegisterEntity (entity);
		}
		private static void RegisterEntity(Entity ent){
			//var id=SceneView.entities.IndexOf (ent);
			tree.AddNode (()=>ent);
		}
		public override void OnMouseMove (OpenTK.Input.MouseMoveEventArgs evnt)
		{
			base.OnMouseMove (evnt);
		}
	}
}

