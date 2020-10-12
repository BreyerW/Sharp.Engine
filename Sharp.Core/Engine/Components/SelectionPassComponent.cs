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
	public class SelectionPassComponent : CommandBufferComponent
	{
		private static Bitask rendererMask = new(0);
		public SelectionPassComponent(Entity parent) : base(parent)
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("selectionScene", TextureRole.Color0, 0, 0, TextureFormat.RGB);
			CreateNewTemporaryTexture("selectionDepth", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
			Material.BindGlobalProperty("alphaThreshold", 0.33f);
			rendererMask.SetFlag(0);
		}

		public override void Execute()
		{
			if (SceneView.locPos.HasValue)
			{
				if (PickTestForGizmo() is false)
				{
					MainWindow.backendRenderer.ClearBuffer();
					List<MeshRenderer> renderables = new List<MeshRenderer>();
					var renderers = Entity.FindAllWithComponentsAndTags(rendererMask, Camera.main.cullingTags, cullTags: true).GetEnumerator();
					int id = 1;
					while (renderers.MoveNext())
						foreach (var r in renderers.Current)
						{
							var renderer = r.GetComponent<MeshRenderer>();
							if (renderer.active is true)
							{
								var color = new Color((byte)((id & 0x000000FF) >> 00), (byte)((id & 0x0000FF00) >> 08), (byte)((id & 0x00FF0000) >> 16), 255);
								renderer.material.BindProperty("colorId", color);
								renderables.Add(renderer);
								id++;
							}
						}
					DrawPass(renderables);
					var pixel = MainWindow.backendRenderer.ReadPixels(SceneView.locPos.Value.x, SceneView.locPos.Value.y - 1 /*locPos.Value.y - 64*/, 1, 1);
					var index = ((pixel[0]) << 00) + ((pixel[1]) << 08) + (((pixel[2]) << 16));
					if (index is not 0)
						Selection.Asset = renderables[index - 1].Parent;
					foreach (var r in renderables)
						r.material.BindProperty("colorId", Color.Transparent);
				}
				SceneView.locPos = null;
			}
		}
		private bool PickTestForGizmo()
		{
			//MainWindow.backendRenderer.SetFlatColorState();
			if (SceneStructureView.tree.SelectedNode != null)
			{
				Color xColor = Color.Red, yColor = Color.LimeGreen, zColor = Color.Blue;
				Color xRotColor = Color.Red, yRotColor = Color.LimeGreen, zRotColor = Color.Blue;
				Color xScaleColor = Color.Red, yScaleColor = Color.LimeGreen, zScaleColor = Color.Blue;
				Color color;
				for (int id = 1; id < 10; id++)
				{
					color = new Color((byte)((id & 0x000000FF) >> 00), (byte)((id & 0x0000FF00) >> 08), (byte)((id & 0x00FF0000) >> 16), 255);
					switch (id)
					{
						case 1:
							xColor = color;
							break;

						case 2:
							yColor = color;
							break;

						case 3:
							zColor = color;
							break;

						case 4:
							xRotColor = color;
							break;

						case 5:
							yRotColor = color;
							break;

						case 6:
							zRotColor = color;
							break;

						case 7:
							xScaleColor = color;
							break;

						case 8:
							yScaleColor = color;
							break;

						case 9:
							zScaleColor = color;
							break;
					}
				}
				DrawPass(null);
				//foreach (var selected in SceneStructureView.tree.SelectedNode)
				if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
				{
					Manipulators.DrawCombinedGizmos(entity, new Vector2(cam.Width, cam.Height), xColor, yColor, zColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);
				}
				if (SceneView.locPos.HasValue)
				{
					var pixel = MainWindow.backendRenderer.ReadPixels(SceneView.locPos.Value.x, SceneView.locPos.Value.y - 1 /*locPos.Value.y - 64*/, 1, 1);
					int index = ((pixel[0]) << 00) + ((pixel[1]) << 08) + (((pixel[2]) << 16));
					Console.WriteLine("encoded index=" + index);
					if (index > 0 && index < 10)
					{
						Manipulators.selectedAxisId = index;
						SceneView.mouseLocked = true;
						return true;
					}
					else
						Manipulators.selectedAxisId = 0;
				}
			}
			return false;
		}
	}
}
