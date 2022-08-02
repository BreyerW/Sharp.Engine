using PluginAbstraction;
using Sharp.Core;
using SharpAsset;
using SharpAsset.AssetPipeline;
using System;
using System.Runtime.CompilerServices;

namespace Sharp.Engine.Components
{
	class DepthPrePassComponent : CommandBufferComponent
	{
		[ModuleInitializer]
		public static void Register()
		{
			ref var mask = ref StaticDictionary<CommandBufferComponent>.Get<BitMask>();
			if (mask.IsDefault)
				mask = new BitMask(0);
			mask.SetFlag(Extension.RegisterComponent<DepthPrePassComponent>());
		}
		protected override void Initialize()
		{
			base.Initialize();
			CreateNewTemporaryTexture("sceneTarget", TextureRole.Color0, 0, 0, TextureFormat.A);
			CreateNewTemporaryTexture("depthTarget", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
			var shader = ShaderPipeline.Import(Application.projectPath + @"\Content\HighlightMaskPassShader.shader");
			swapMaterial = new Material();
			swapMaterial.BindShader(0, shader);
			ScreenSpace = true;
		}
		public override void Execute()
		{
			PluginManager.backendRenderer.EnableState(RenderState.DepthMask);
			DrawPass();
			PluginManager.backendRenderer.DisableState(RenderState.DepthMask);
		}
	}
}
