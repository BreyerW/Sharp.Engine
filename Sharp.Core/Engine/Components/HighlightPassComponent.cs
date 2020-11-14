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
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		internal static Material viewCubeMat;
		private static Material selectedMaterial;
		private static Material hoveredMaterial;
		public HighlightPassComponent(Entity parent) : base(parent)
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("highlightScene", TextureRole.Color0, 0, 0, TextureFormat.RGB);//TODO: convert this to GL_R32UI or GL_RB32UI, more than 64 bit is unlikely to be necessary, also optimize size to 1 pixel and rect when multiselecting
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
				if (asset is Entity ent)
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
				swapMaterial = hoveredMaterial;
				GL.Disable(EnableCap.DepthTest);
				//GL.DepthFunc(DepthFunction.Always);
				Draw(hovered);
				swapMaterial = null;
				Material.BindGlobalProperty("colorId", new Color(0, 255, 0, 255));
				Draw(hoveredWithTransparency);
			}
			else if (Manipulators.hoveredGizmoId < Gizmo.TranslateX)
				viewCubeMat.Draw(1);
			else
			{
				GL.Disable(EnableCap.DepthTest);
				var editorHovered = new Color(0, 0, 255, 255);
				if (SceneStructureView.tree.SelectedNode is not null)
				{
					//MainWindow.backendRenderer.WriteDepth(true);
					//MainWindow.backendRenderer.ClearDepth();
					//foreach (var selected in SceneStructureView.tree.SelectedNode)
					if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
					{
						Material material = Manipulators.hoveredGizmoId switch
						{
							Gizmo.TranslateX =>
								DrawHelper.lineMaterialX,
							Gizmo.TranslateY =>
								DrawHelper.lineMaterialY,
							Gizmo.TranslateZ =>
								DrawHelper.lineMaterialZ,
							Gizmo.TranslateXY =>
								DrawHelper.planeMaterialXY,
							Gizmo.TranslateYZ =>
								DrawHelper.planeMaterialYZ,
							Gizmo.TranslateZX =>
								DrawHelper.planeMaterialZX,
							Gizmo.RotateX =>
								DrawHelper.circleMaterialX,
							Gizmo.RotateY =>
								DrawHelper.circleMaterialY,
							Gizmo.RotateZ =>
								DrawHelper.circleMaterialZ,
							Gizmo.ScaleX =>
								DrawHelper.cubeMaterialX,
							Gizmo.ScaleY =>
								DrawHelper.cubeMaterialY,
							Gizmo.ScaleZ =>
								DrawHelper.cubeMaterialZ,
							_ => null
						};
						Material extra = Manipulators.hoveredGizmoId switch
						{
							Gizmo.TranslateX =>
								DrawHelper.coneMaterialX,
							Gizmo.TranslateY =>
								DrawHelper.coneMaterialY,
							Gizmo.TranslateZ =>
								DrawHelper.coneMaterialZ,
							_ => null
						};
						extra?.BindProperty("color", editorHovered);
						extra?.Draw();
						material?.BindProperty("color", editorHovered);
						material?.Draw();
					}
				}
				Manipulators.ResetGizmoColors();
			}
			GL.Enable(EnableCap.DepthTest);
			//if (readPixels is not -1)
			Material.BindGlobalProperty("enablePicking", 0f);
			//	readPixels--;

		}
	}
}
