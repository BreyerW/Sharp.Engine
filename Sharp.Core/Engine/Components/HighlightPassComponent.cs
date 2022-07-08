using PluginAbstraction;
using Sharp.Core;
using Sharp.Editor;
using SharpAsset;
using SharpAsset.AssetPipeline;
using System;
using System.Collections.Generic;

namespace Sharp.Engine.Components
{
	public class HighlightPassComponent : CommandBufferComponent
	{
		internal static Material viewCubeMat;
		private static Material selectedMaterial;
		private static Material hoveredMaterial;
		public HighlightPassComponent(Entity parent) : base(parent)
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("highlightScene", TextureRole.Color0, 0, 0, TextureFormat.R);
			CreateNewTemporaryTexture("highlightDepth", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);


			if (selectedMaterial is null)
			{
				selectedMaterial = new Material();
				var shader = ShaderPipeline.Import(Application.projectPath + @"\Content\SelectedMaskPassShader.shader");
				selectedMaterial.BindShader(0, shader);

				hoveredMaterial = new Material();
				shader = ShaderPipeline.Import(Application.projectPath + @"\Content\HoveredMaskPassShader.shader");
				hoveredMaterial.BindShader(0, shader);
			}

			Material.BindGlobalProperty("alphaThreshold", 0.33f);
		}

		public override void Execute()
		{
			PreparePass();
			Material.BindGlobalProperty("enablePicking", 1f);
			List<Renderer> selected = new();
			List<Renderer> hovered = new();
			List<Renderer> selectedWithTransparency = new();
			List<Renderer> hoveredWithTransparency = new();

			if (Manipulators.selectedGizmoId is Gizmo.Invalid && Selection.HoveredObject is Entity e)
			{
				var renderer = e.GetComponent<Renderer>();
				if (renderer.material.IsBlendRequiredForPass(0))
					hoveredWithTransparency.Add(renderer);
				else
					hovered.Add(renderer);
			}

			foreach (var asset in Selection.selectedAssets)
				if (asset is Entity ent && ent.name is not "Main Camera" && ent.ComponentsMask.HasAnyFlags(rendererMask))
				{
					var renderer = ent.GetComponent<Renderer>();
					if (renderer.material.IsBlendRequiredForPass(0))
						selectedWithTransparency.Add(renderer);
					else
						selected.Add(renderer);
				}
			swapMaterial = selectedMaterial;
			Draw(selected);
			swapMaterial = null;
			Material.BindGlobalProperty("colorId", new Color(255, 0, 0, 255));
			Draw(selectedWithTransparency);
			if (Manipulators.hoveredGizmoId is Gizmo.Invalid)
			{
				if (hovered.Count is 0 && hoveredWithTransparency.Count is 0)
				{
					//GL.Enable(EnableCap.DepthTest);
					Material.BindGlobalProperty("enablePicking", 0f);
					return;
				}
				swapMaterial = hoveredMaterial;
				PluginManager.backendRenderer.DisableState(RenderState.DepthTest);
				//GL.DepthFunc(DepthFunction.Always);
				Draw(hovered);
				swapMaterial = null;
				Material.BindGlobalProperty("colorId", new Color(127, 0, 0, 255));
				Draw(hoveredWithTransparency);
				PluginManager.backendRenderer.EnableState(RenderState.DepthTest);
			}
			Material.BindGlobalProperty("enablePicking", 0f);

		}
	}
}
