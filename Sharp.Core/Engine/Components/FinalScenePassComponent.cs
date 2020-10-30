using Sharp.Editor;
using Sharp.Engine.Components;
using SharpAsset;
using SharpAsset.Pipeline;
using System;
using OpenTK.Graphics.OpenGL;
using Sharp.Editor.Views;
using System.Numerics;

namespace Sharp.Core.Engine.Components
{
	class FinalScenePassComponent : CommandBufferComponent
	{
		private Material highlight;
		public FinalScenePassComponent(Entity parent) : base(parent)
		{
			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightShader.shader");
			ref var screenMesh = ref Pipeline.Get<Mesh>().GetAsset("screen_space_square");
			ref var selectionTexture = ref Pipeline.Get<Texture>().GetAsset("selectionTarget");
			ref var sceneDepthTexture = ref Pipeline.Get<Texture>().GetAsset("depthTarget");
			ref var selectionDepthTexture = ref Pipeline.Get<Texture>().GetAsset("selectionDepthTarget");
			highlight = new Material();
			highlight.BindShader(0, shader);
			highlight.BindProperty("MyTexture", selectionTexture);
			highlight.BindProperty("SelectionDepthTex", selectionDepthTexture);
			highlight.BindProperty("SceneDepthTex", sceneDepthTexture);
			highlight.BindProperty("outline_color", Manipulators.selectedColor);
			highlight.BindProperty("mesh", screenMesh);
		}

		public override void Execute()
		{
			GL.Enable(EnableCap.Blend);
			DrawPass();

			//blit from SceneCommand to this framebuffer instead
			DrawHelper.DrawGrid(Camera.main.Parent.transform.Position);
			MainWindow.backendRenderer.WriteDepth(false);

			highlight.Draw();


			if (SceneStructureView.tree.SelectedNode?.UserData is Entity e)
			{
				//foreach (var selected in SceneStructureView.tree.SelectedChildren)
				{
					MainWindow.backendRenderer.WriteDepth(true);
					MainWindow.backendRenderer.ClearDepth();

					Manipulators.DrawCombinedGizmos(e);
					MainWindow.backendRenderer.WriteDepth(false);
				}
			}
		}
	}
}
