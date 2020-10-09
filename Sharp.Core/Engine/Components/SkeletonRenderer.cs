using System;
using Sharp.Editor.Views;
using SharpAsset;
using System.Numerics;

namespace Sharp
{
	public class SkeletonRenderer : Renderer
	{
		public Skeleton skele;

		public SkeletonRenderer(Entity parent) : base(parent)
		{
		}

		public void Initialize(ref Skeleton Skele)
		{
			skele = Skele;
			Allocate();
		}


		private void Allocate()
		{
			MainEditorView.editorBackendRenderer.newinit(ref skele);
			MainEditorView.editorBackendRenderer.update(ref skele);
			MainEditorView.editorBackendRenderer.display(ref skele);
		}

		public override void Render()
		{
			MainEditorView.editorBackendRenderer.update(ref skele);
			MainEditorView.editorBackendRenderer.display(ref skele);
		}

		/* public override void SetupMatrices()
         {
             entityObject.Scale = new Vector3(20, 20, 20);
             entityObject.SetModelMatrix();
             skele.MVP = entityObject.ModelMatrix * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
         }*/
	}
}