using BepuUtilities;
using PluginAbstraction;
using Sharp.Core;
using SharpAsset;
using SharpAsset.AssetPipeline;
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
