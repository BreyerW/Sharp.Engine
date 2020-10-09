using Sharp.Core;
using SharpAsset;
using SharpAsset.Pipeline;
using SharpSL;
using SharpSL.BackendRenderers;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
namespace Sharp.Engine.Components
{
	//TODO: requires CameraComponent
	public abstract class CommandBufferComponent : Component//Renderer?
	{
		private static BitMask rendererMask = new BitMask(0);
		private bool screenSpace;
		public bool ScreenSpace
		{
			set
			{
				if (value)
				{
					var cam = Parent.GetComponent<Camera>();
					cam.OnDimensionChanged += OnCameraSizeChange;
				}
				else
				{
					var cam = Parent.GetComponent<Camera>();
					cam.OnDimensionChanged -= OnCameraSizeChange;
				}
				screenSpace = value;
			}
			get => screenSpace;
		}
		internal int FBO = -1;
		internal List<(int texId, TextureRole role)> targetTextures = new();//TODO: make tag&layers (maybe IdentityComponent?) component and use that for render to texture (eg. selected tag or layer)
		public List<Material> passes;

		protected CommandBufferComponent(Entity parent) : base(parent)
		{
			rendererMask.SetFlag(0);
		}

		private void OnCameraSizeChange(Camera cam)
		{
			foreach (var t in targetTextures)
			{
				ref var tex = ref Pipeline.Get<Texture>().GetAsset(t.texId);
				tex.width = cam.Width;
				tex.height = cam.Height;
				MainWindow.backendRenderer.BindBuffers(Target.Texture, tex.TBO);
				MainWindow.backendRenderer.Allocate(ref tex.bitmap is null ? ref Unsafe.NullRef<byte>() : ref tex.bitmap[0], tex.width, tex.height, tex.format);
			}
		}
		protected void CreateNewTemporaryTexture(string name, TextureRole role, int width, int height, TextureFormat pixFormat)
		{
			var tex = new Texture()
			{
				width = width,
				height = height,
				format = pixFormat,
				bitmap = null,
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
		private void PreparePass()
		{
			ref var tex = ref Pipeline.Get<Texture>().GetAsset(targetTextures[0].texId);
			MainWindow.backendRenderer.Viewport(0, 0, tex.width, tex.height);
			MainWindow.backendRenderer.Clip(0, 0, tex.width, tex.height);
			MainWindow.backendRenderer.BindBuffers(Target.Frame, FBO);
			MainWindow.backendRenderer.SetStandardState();
			MainWindow.backendRenderer.ClearBuffer();
		}
		public void DrawPass(BitMask mask)
		{
			PreparePass();
			var renderables = Entity.FindAllWithComponentsAndTags(rendererMask, mask).GetEnumerator();
			while (renderables.MoveNext())
				foreach (var renderable in renderables.Current)
				{
					var renderer = renderable.GetComponent<Renderer>();
					if (renderer.active is true)
						renderer.Render();
				}

		}
		public void DrawPass(IEnumerable<Renderer> renderables)
		{
			PreparePass();
			foreach (var renderable in renderables)
			{
				if (renderable.active is true)
					renderable.Render();
			}

		}
		public void DrawPass()
		{
			PreparePass();
			var cam = Parent.GetComponent<Camera>();
			var renderables = Entity.FindAllWithComponentsAndTags(rendererMask, cam.cullingTags, cullTags: true).GetEnumerator();
			while (renderables.MoveNext())
				foreach (var renderable in renderables.Current)
				{
					var renderer = renderable.GetComponent<Renderer>();
					//if (renderer is null) continue;
					if (renderer.active is true)
						renderer.Render();
				}

			//TODO: draw all meshes from camera culling tags optionally swap material
		}
		public abstract void Execute();

	}
}
