using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using SharpSL;
using SharpSL.BackendRenderers;
using System;
using OpenTK.Graphics.OpenGL;

namespace Sharp.Engine.Components
{
	class SelectionCommandComponent : CommandBufferComponent
	{
		public SelectionCommandComponent()
		{
			CreateNewTemporaryTexture("selectionTarget", TextureRole.Color0, 0, 0, TextureFormat.A);
			CreateNewTemporaryTexture("selectionDepthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
		}
		public override void Execute()
		{
			DrawPass();
			MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			//Material tmpMat;
			//GL.Disable(EnableCap.AlphaTest);
			if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
			{
				//foreach (var selected in SceneStructureView.tree.SelectedChildren)
				{
					var r = entity.GetComponent<MeshRenderer>();
					if (r is not null)
					{
						//tmpMat = r.SwapMaterial(highlightFirstStage);
						r.Render();
						//r.material = tmpMat;
					}
					/*dynamic renderer = entity.GetComponent(typeof(MeshRenderer<,>));
					if (renderer != null)
					{
						var max = renderer.mesh.bounds.Max;
						var min = renderer.mesh.bounds.Min;
						DrawHelper.DrawBox(min, max);
					}*/
				}
			}
		}
	}
}
