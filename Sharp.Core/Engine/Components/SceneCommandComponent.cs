using SharpAsset;
using SharpSL.BackendRenderers;
using System;

namespace Sharp.Engine.Components
{
	class SceneCommandComponent : CommandBufferComponent
	{
		public SceneCommandComponent(Entity p) : base(p)
		{
			CreateNewTemporaryTexture("sceneTarget", TextureRole.Color0, 0, 0, TextureFormat.RGBA);
			CreateNewTemporaryTexture("depthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
		}
		public override void Execute()
		{
			DrawPass();
			//MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			//GL.Enable(EnableCap.Blend);

			//GL.Disable(EnableCap.Blend);
			//GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
		}
	}
}
