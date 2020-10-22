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
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		private List<MeshRenderer> renderables;
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
			/*if (readPixels is 0)
			{
				Console.WriteLine(FBO);
				BindFrame();
				
				readPixels = -1;
			}*/
			Material.BindGlobalProperty("enablePicking", 1f);
			renderables = new List<MeshRenderer>();
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
			//if (readPixels is 0)
			{
				var pixel = MainWindow.backendRenderer.ReadPixels((int)SceneView.localMousePos.X, (int)SceneView.localMousePos.Y - 1, 1, 1);//TODO: optimize using PBO
				var index = ((pixel[0]) << 00) + ((pixel[1]) << 08) + (((pixel[2]) << 16));
				if (SceneView.locPos.HasValue)
				{
					if (index is not 0)
					{
						Camera.main.pivot = renderables[index - 1].Parent.transform.Position;
						Selection.Asset = renderables[index - 1].Parent;
					}
					SceneView.locPos = null;
				}
				else if (index is not 0)
				{
					Selection.HoveredObject = renderables[index - 1].Parent;
				}
				else
				{
					Selection.HoveredObject = null;
				}
				//readPixels = 2;
			}

			//if (readPixels is not -1)
			//	readPixels--;
			Material.BindGlobalProperty("enablePicking", 0f);
		}

	}
}
