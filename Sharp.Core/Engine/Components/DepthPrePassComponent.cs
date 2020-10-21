﻿using BepuUtilities;
using SharpAsset;
using SharpAsset.Pipeline;
using SharpSL.BackendRenderers;
using System;

namespace Sharp.Engine.Components
{
	class DepthPrePassComponent : CommandBufferComponent
	{
		public DepthPrePassComponent(Entity p) : base(p)
		{
			CreateNewTemporaryTexture("sceneTarget", TextureRole.Color0, 0, 0, TextureFormat.A);
			CreateNewTemporaryTexture("depthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightMaskPassShader.shader");
			swapMaterial = new Material();
			swapMaterial.Shader = shader;
		}
		public override void Execute()
		{
			MainWindow.backendRenderer.WriteDepth(true);
			DrawPass();
			MainWindow.backendRenderer.WriteDepth(false);

		}
	}
}
