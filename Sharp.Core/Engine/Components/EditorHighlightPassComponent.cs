using OpenTK.Graphics.OpenGL;
using Sharp.Core;
using Sharp.Editor;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using SharpAsset;
using SharpSL.BackendRenderers;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Sharp.Engine.Components
{
	public class EditorHighlightPassComponent : CommandBufferComponent
	{
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		internal static Material viewCubeMat;
		public EditorHighlightPassComponent(Entity parent) : base(parent)
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("editorSelectionScene", TextureRole.Color0, 0, 0, TextureFormat.RGB);
			CreateNewTemporaryTexture("editorSelectionDepth", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
			Material.BindGlobalProperty("alphaThreshold", 0.33f);
		}

		public override void Execute()
		{
			/*if (readPixels is 0)
			{
				Console.WriteLine(FBO);
				BindFrame();
				
				readPixels = -1;
			}*/
			//
			//MainWindow.backendRenderer.ClearBuffer();
			//if (SceneView.mouseLocked) return;
			Material.BindGlobalProperty("enablePicking", 1f);
			PreparePass();
			GL.Enable(EnableCap.Blend);
			//var inactive = new Color(255, 255, 255, 255);
			var hovered = new Color(0, 0, 255, 255);
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
					extra?.BindProperty("color", hovered);
					extra?.Draw();
					material?.BindProperty("color", hovered);
					material?.Draw();
				}
			}
			Manipulators.ResetGizmoColors();
			viewCubeMat.Draw(1);
			//MainWindow.backendRenderer.WriteDepth(true);
			//if (readPixels is not -1)
			//	readPixels--;
			Material.BindGlobalProperty("enablePicking", 0f);

		}
	}
}
