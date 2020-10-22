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
	public class EditorSelectionPassComponent : CommandBufferComponent
	{
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		internal static Material viewCubeMat;
		internal static Material editorHighlight;
		public EditorSelectionPassComponent(Entity parent) : base(parent)
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
			PreparePass();
			MainWindow.backendRenderer.ClearBuffer();
			if (SceneView.mouseLocked) return;
			Material.BindGlobalProperty("enablePicking", 1f);
			if (SceneStructureView.tree.SelectedNode is not null)
			{
				Color xColor = Color.Red, yColor = Color.LimeGreen, zColor = Color.Blue;
				Color xPlaneColor = Color.Red, yPlaneColor = Color.LimeGreen, zPlaneColor = Color.Blue;
				Color xRotColor = Color.Red, yRotColor = Color.LimeGreen, zRotColor = Color.Blue;
				Color xScaleColor = Color.Red, yScaleColor = Color.LimeGreen, zScaleColor = Color.Blue;
				Color color;
				for (int id = 27; id < 39; id++)
				{
					color = new Color((byte)((id & 0x000000FF) >> 00), (byte)((id & 0x0000FF00) >> 08), (byte)((id & 0x00FF0000) >> 16), 255);
					switch (id)
					{
						case 27:
							xColor = color;
							break;

						case 28:
							yColor = color;
							break;

						case 29:
							zColor = color;
							break;
						case 30:
							xPlaneColor = color;
							break;

						case 31:
							yPlaneColor = color;
							break;

						case 32:
							zPlaneColor = color;
							break;
						case 33:
							xRotColor = color;
							break;

						case 34:
							yRotColor = color;
							break;

						case 35:
							zRotColor = color;
							break;

						case 36:
							xScaleColor = color;
							break;

						case 37:
							yScaleColor = color;
							break;

						case 38:
							zScaleColor = color;
							break;
					}
				}
				MainWindow.backendRenderer.WriteDepth(true);
				MainWindow.backendRenderer.ClearDepth();
				//foreach (var selected in SceneStructureView.tree.SelectedNode)
				if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
				{
					Manipulators.DrawCombinedGizmos(entity, xColor, yColor, zColor, xPlaneColor, yPlaneColor, zPlaneColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);
				}
			}
			viewCubeMat.SendData();
			//if (readPixels is 0)
			{
				var pixel = MainWindow.backendRenderer.ReadPixels((int)SceneView.localMousePos.X, (int)SceneView.localMousePos.Y - 1, 1, 1);//TODO: optimize using PBO
				var index = ((pixel[0]) << 00) + ((pixel[1]) << 08) + (((pixel[2]) << 16));
				if (SceneView.locPos.HasValue && index is not 0)
				{
					if (index < 27)

						Manipulators.selectedGizmoId = (Gizmo)index;

					else
						SceneView.mouseLocked = true;
					SceneView.locPos = null;
				}
				else if (index is not 0)
				{
					if (index < 27)
						viewCubeMat.BindProperty("isHovered", 1f);

					Manipulators.selectedGizmoId = (Gizmo)index;
					editorHighlight.BindProperty("hoverIdColor", new Color(pixel[0], 0, 0, 255));
				}
				else
				{
					if (SceneView.mouseLocked is false)
					{
						Manipulators.selectedGizmoId = Gizmo.Invalid;
						editorHighlight.BindProperty("hoverIdColor", new Color(0, 0, 0, 0));
						viewCubeMat.BindProperty("isHovered", 0f);
					}
				}
				//readPixels = 2;
			}

			//if (readPixels is not -1)
			//	readPixels--;
			Material.BindGlobalProperty("enablePicking", 0f);
		}
	}
}
