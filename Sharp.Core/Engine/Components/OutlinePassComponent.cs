using Microsoft.VisualBasic;
using Sharp.Core;
using Sharp.Editor.Views;
using SharpAsset;
using SharpSL.BackendRenderers;
using System.Collections.Generic;

namespace Sharp.Engine.Components
{
	class OutlinePassComponent : CommandBufferComponent
	{
		public OutlinePassComponent(Entity p) : base(p)
		{
			CreateNewTemporaryTexture("selectionTarget", TextureRole.Color0, 0, 0, TextureFormat.A);
			CreateNewTemporaryTexture("selectionDepthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
		}
		public override void Execute()
		{
			List<MeshRenderer> renderables = new();
			foreach (var asset in Selection.Assets)
				if (asset is Entity e && e.GetComponent<MeshRenderer>() is MeshRenderer renderer)
				{
					renderables.Add(renderer);
				}
			DrawPass(renderables);
			/*MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			//Material tmpMat;
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
					}*
				}
			}*/
		}
	}
}
