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

namespace Sharp.Engine.Components
{
	public class HighlightPassComponent : CommandBufferComponent
	{
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		public HighlightPassComponent(Entity parent) : base(parent)
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("highlightScene", TextureRole.Color0, 0, 0, TextureFormat.RGB);//TODO: convert this to GL_R32UI or GL_RB32UI, more than 64 bit is unlikely to be necessary, also optimize size to 1 pixel and rect when multiselecting
			CreateNewTemporaryTexture("highlightDepth", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
		}

		public override void Execute()
		{
			PreparePass();
			Material.BindGlobalProperty("enablePicking", 1f);
			List<Entity> selected = new();
			List<Entity> hovered = new();

			if (Manipulators.SelectedGizmoId is Gizmo.Invalid && Selection.HoveredObject is Entity e)
			{
				e.GetComponent<MeshRenderer>()?.material.BindProperty("colorId", new Color(0, 255, 0, 255));
				hovered.Add(e);
			}

			foreach (var asset in Selection.Assets)
				if (asset is Entity ent)
				{
					ent.GetComponent<MeshRenderer>()?.material.BindProperty("colorId", new Color(255, 0, 0, 255));
					selected.Add(ent);
				}

			Draw(selected);
			GL.Disable(EnableCap.DepthTest);
			Draw(hovered);
			GL.Enable(EnableCap.DepthTest);
			//if (readPixels is not -1)
			Material.BindGlobalProperty("enablePicking", 0f);
			//	readPixels--;

		}
	}
}
