using Newtonsoft.Json;
using Sharp.Core;
using SharpAsset;
using SharpAsset.Pipeline;
using SharpSL;
using SharpSL.BackendRenderers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;

namespace Sharp.Engine.Components
{
	//TODO: requires CameraComponent
	public abstract class CommandBufferComponent : Component//Renderer?
	{
		protected Camera cam;
		private CommandBufferComponent prevPass = null;
		[JsonProperty]
		public CommandBufferComponent PrevPass
		{
			get => prevPass;
			internal set
			{
				prevPass = value;
			}
		}
		private static Bitask rendererMask = new Bitask(0);
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
			rendererMask.SetFlag(0);
			cam = Parent.GetComponent<Camera>();
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
			//MainWindow.backendRenderer.Viewport(0, 0, width, height);
			//MainWindow.backendRenderer.Clip(0, 0, width, height);
			MainWindow.backendRenderer.BindBuffers(Target.Frame, FBO);
			//MainWindow.backendRenderer.SetStandardState();
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
			MainWindow.backendRenderer.Viewport(0, 0, width, height);
			MainWindow.backendRenderer.Clip(0, 0, width, height);
			MainWindow.backendRenderer.BindBuffers(Target.Frame, FBO);

			MainWindow.backendRenderer.SetStandardState();
			MainWindow.backendRenderer.ClearBuffer();
			MainWindow.backendRenderer.ClearColor(0f, 0f, 0f, 0f);

		}
		public void DrawPass(Bitask mask)
		{
			PreparePass();
			var renderables = Entity.FindAllWithComponentsAndTags(rendererMask, mask).GetEnumerator();
			while (renderables.MoveNext())
				Draw(renderables.Current);
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
			var renderables = Entity.FindAllWithComponentsAndTags(rendererMask, cam.cullingTags, cullTags: true).GetEnumerator();
			while (renderables.MoveNext())
				Draw(renderables.Current);

		}
		internal void Draw(IReadOnlyCollection<Entity> renderables)
		{
			var condition = swapMaterial is not null;
			foreach (var renderable in renderables)
			{
				var renderer = renderable.GetComponent<MeshRenderer>();
				if (renderer is { active: true })
				{
					if (condition)
					{
						var tmp = renderer.SwapMaterial(swapMaterial);
						renderer.Render();
						renderer.SwapMaterial(tmp);
					}
					else
						renderer.Render();
				}
			}
		}
		public abstract void Execute();

	}
}
