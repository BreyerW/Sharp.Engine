using Sharp.Core;
using Sharp.Editor;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using SharpAsset;
using SharpSL.BackendRenderers;
using System;
using System.Collections.Generic;
using System.Numerics;
using OpenTK.Graphics.OpenGL;
using SharpAsset.Pipeline;

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
				var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\SelectedMaskPassShader.shader");
				selectedMaterial.BindShader(0, shader);

				hoveredMaterial = new Material();
				shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HoveredMaskPassShader.shader");
				hoveredMaterial.BindShader(0, shader);
			}

			Material.BindGlobalProperty("alphaThreshold", 0.33f);
		}

		public override void Execute()
		{
			PreparePass();
			Material.BindGlobalProperty("enablePicking", 1f);
			List<Entity> selected = new();
			List<Entity> hovered = new();
			List<Entity> selectedWithTransparency = new();
			List<Entity> hoveredWithTransparency = new();

			if (Manipulators.SelectedGizmoId is Gizmo.Invalid && Selection.HoveredObject is Entity e)
			{
				if (e.GetComponent<MeshRenderer>().material.IsMainPassTransparent)
					hoveredWithTransparency.Add(e);
				else
					hovered.Add(e);
			}

			foreach (var asset in Selection.Assets)
				if (asset is Entity ent && ent.name is not "Main Camera")
				{
					if (ent.GetComponent<MeshRenderer>().material.IsMainPassTransparent)
						selectedWithTransparency.Add(ent);
					else
						selected.Add(ent);
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
				GL.Disable(EnableCap.DepthTest);
				//GL.DepthFunc(DepthFunction.Always);
				Draw(hovered);
				swapMaterial = null;
				Material.BindGlobalProperty("colorId", new Color(0, 255, 0, 255));
				Draw(hoveredWithTransparency);
				GL.Enable(EnableCap.DepthTest);
			}
			Material.BindGlobalProperty("enablePicking", 0f);

		}
	}
}
