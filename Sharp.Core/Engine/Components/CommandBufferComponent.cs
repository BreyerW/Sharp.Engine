
using PluginAbstraction;
using Sharp.Core;
using Sharp.Physic;
using SharpAsset;
using SharpAsset.AssetPipeline;
using SharpSL;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Sharp.Engine.Components
{
    //TODO: requires CameraComponent
    public abstract class CommandBufferComponent : Component//Renderer?
    {
        protected Camera cam;
        private CommandBufferComponent prevPass = null;
        [JsonInclude]
        //[JsonProperty]
        public CommandBufferComponent PrevPass
        {
            get => prevPass;
            internal set
            {
                prevPass = value;
            }
        }
        internal static BitMask rendererMask = new BitMask(0);
        private bool screenSpace;
        public bool ScreenSpace
        {
            set
            {
                if (value)
                {
                    cam.OnDimensionChanged += OnCameraSizeChange;
                }
                else
                {
                    cam.OnDimensionChanged -= OnCameraSizeChange;
                }
                screenSpace = value;
            }
            get => screenSpace;
        }
        internal int FBO = -1;
        internal List<(int texId, TextureRole role)> targetTextures = new();
        public Material swapMaterial;
        protected CommandBufferComponent(Entity parent) : base(parent)
        {
            rendererMask = Extension.GetBitMaskFor<Renderer>();
            cam = Parent.GetComponent<Camera>();
        }

        private void OnCameraSizeChange(Camera cam)
        {
            foreach (var t in targetTextures)
            {
                ref var tex = ref Pipeline.Get<Texture>().GetAsset(t.texId);
                tex.width = cam.Width;
                tex.height = cam.Height;
                PluginManager.backendRenderer.BindBuffers(Target.Texture, tex.TBO);
                PluginManager.backendRenderer.Allocate(ref tex.data is null ? ref Unsafe.NullRef<byte>() : ref tex.data[0], tex.width, tex.height, tex.format);
            }
        }
        protected void CreateNewTemporaryTexture(string name, TextureRole role, int width, int height, TextureFormat pixFormat)
        {
            var tex = new Texture()
            {
                width = width,
                height = height,
                format = pixFormat,
                data =GC.AllocateUninitializedArray<byte>(Unsafe.SizeOf<int>(),true),
                FullPath = $"{name}.generated",
                TBO = -1,
            };
            var texId = Pipeline.Get<Texture>().Register(tex);
            targetTextures.Add((texId, role));
        }

        protected void ReuseTemporaryTexture(string texName, TextureRole role)
        {
            ref var tex = ref Pipeline.Get<Texture>().GetAsset(texName);
            var texId = Pipeline.Get<Texture>().Register(tex);
            targetTextures.Add((texId, role));
        }
        protected void BindFrame()
        {
            int width, height;
            if (screenSpace)
            {
                width = cam.Width;
                height = cam.Height;
            }
            else
            {
                ref var tex = ref Pipeline.Get<Texture>().GetAsset(targetTextures[0].texId);
                width = tex.width;
                height = tex.height;
            }
            //PluginManager.backendRenderer.Viewport(0, 0, width, height);
            //PluginManager.backendRenderer.Clip(0, 0, width, height);
            PluginManager.backendRenderer.BindBuffers(Target.Frame, FBO);
            //PluginManager.backendRenderer.SetStandardState();
        }
        protected void PreparePass()
        {
            int width, height;
            if (screenSpace)
            {
                width = cam.Width;
                height = cam.Height;
            }
            else
            {
                ref var tex = ref Pipeline.Get<Texture>().GetAsset(targetTextures[0].texId);
                width = tex.width;
                height = tex.height;
            }
            PluginManager.backendRenderer.Viewport(0, 0, width, height);
            PluginManager.backendRenderer.Clip(0, 0, width, height);
            PluginManager.backendRenderer.BindBuffers(Target.Frame, FBO);

            PluginManager.backendRenderer.EnableState(RenderState.DepthMask | RenderState.DepthTest);
            PluginManager.backendRenderer.ClearBuffer();
            PluginManager.backendRenderer.ClearColor(0f, 0f, 0f, 0f);

        }
        public void DrawPass(BitMask mask)
        {
            PreparePass();
            var renderers = new List<Renderer>();
            foreach (var i in ..CollisionDetection.inFrustumLength)
            {
                var entity = CollisionDetection.inFrustum[i].GetInstanceObject<Entity>();
                if (entity.ComponentsMask.HasAnyFlags(rendererMask) && entity.TagsMask.HasNoFlags(Camera.main.cullingTags))
                {
                    var renderer = entity.GetComponent<MeshRenderer>();
                    renderers.Add(renderer);
                    /*if (renderer.material.IsBlendRequiredForPass(0))
						transparentRenderables.Add(renderer);
					else
						renderables.Add(renderer);*/
                }
            }
            Draw(renderers);
        }
        public void DrawPass(IEnumerable<MeshRenderer> renderables)
        {
            PreparePass();
            if (renderables is null) return;
            var condition = swapMaterial is not null;
            foreach (var renderable in renderables)
            {
                if (renderable.active is true)
                {
                    if (condition)
                    {
                        var tmp = renderable.SwapMaterial(swapMaterial);
                        renderable.Render();
                        renderable.SwapMaterial(tmp);
                    }
                    else
                        renderable.Render();
                }
            }
        }
        public void DrawPass()
        {
            PreparePass();
            var renderers = new List<Renderer>();
            foreach (var i in ..CollisionDetection.inFrustumLength)
            {
                var entity = CollisionDetection.inFrustum[i].GetInstanceObject<Entity>();
                if (entity.ComponentsMask.HasAnyFlags(rendererMask) && entity.TagsMask.HasNoFlags(Camera.main.cullingTags))
                {
                    var renderer = entity.GetComponent<MeshRenderer>();
                    renderers.Add(renderer);
                    /*if (renderer.material.IsBlendRequiredForPass(0))
						transparentRenderables.Add(renderer);
					else
						renderables.Add(renderer);*/
                }
            }
            Draw(renderers);
        }
        internal void Draw(IReadOnlyCollection<Renderer> renderables)
        {
            var condition = swapMaterial is not null;
            foreach (var renderable in renderables)
            {
                if (renderable is { active: true })
                {
                    if (condition)
                    {
                        var tmp = renderable.SwapMaterial(swapMaterial);
                        renderable.Render();
                        renderable.SwapMaterial(tmp);
                    }
                    else
                        renderable.Render();
                }
            }
        }
        public abstract void Execute();

    }
}
