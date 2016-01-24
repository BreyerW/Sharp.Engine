using System;
using Sharp.Editor.Views;
using SharpAsset;

namespace Sharp
{
	public class SkeletonRenderer:Renderer
	{
		public Skeleton skele;

		public SkeletonRenderer (ref Skeleton Skele)
		{
			
			skele = Skele;
			Allocate ();
		}
		void Allocate(){
			MainEditorView.editorBackendRenderer.newinit (ref skele);
			MainEditorView.editorBackendRenderer.update (ref skele);
			MainEditorView.editorBackendRenderer.display (ref skele);
		}
		public override void Render ()
		{
			SetupMatrices ();
			MainEditorView.editorBackendRenderer.update (ref skele);
			MainEditorView.editorBackendRenderer.display (ref skele);
		}
		public override void SetupMatrices ()
		{
			entityObject.Scale = new OpenTK.Vector3 (20,20,20);
			entityObject.SetModelMatrix ();
			skele.MVP =entityObject.ModelMatrix*Camera.main.modelViewMatrix*Camera.main.projectionMatrix;
		}
	}
}

