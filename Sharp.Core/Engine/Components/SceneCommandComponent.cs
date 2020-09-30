using SharpAsset;
using SharpSL.BackendRenderers;
using System;
using OpenTK.Graphics.OpenGL;
using Sharp.Editor;//TODO remove this dependency, probably via layer/tag system with selected tag or layer

namespace Sharp.Engine.Components
{
	class SceneCommandComponent : CommandBufferComponent
	{
		public SceneCommandComponent()
		{
			CreateNewTemporaryTexture("sceneTarget", TextureRole.Color0, 0, 0, TextureFormat.RGBA);
			CreateNewTemporaryTexture("depthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
		}
		public override void Execute()
		{
			DrawPass();
			MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			GL.Enable(EnableCap.Blend);

			foreach (var renderer in Extension.entities.renderers)
				if (renderer.enabled)
					renderer.Render();
			MainWindow.backendRenderer.WriteDepth(false);
			DrawHelper.DrawGrid(Camera.main.Parent.transform.Position);
			MainWindow.backendRenderer.WriteDepth(true);
			GL.Disable(EnableCap.Blend);
		}
	}
}
