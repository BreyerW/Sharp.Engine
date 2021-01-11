using Microsoft.Toolkit.HighPerformance.Extensions;
using OpenTK.Graphics.OpenGL;
using Sharp.Core;
using Sharp.Editor;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using SharpAsset;
using SharpSL;
using SharpSL.BackendRenderers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

/*namespace Sharp.Engine.Components
{
	public class SelectionPassComponent : CommandBufferComponent//TODO: use depth prepass to guide where alpha is and avoid expensive rerender?
	{
		private static Bitask rendererMask = new(0);
		private int readPixels = 2;
		private (int x, int y) mouseClick;
		internal static (int x, int y, int width, int height) clip;
		//private List<MeshRenderer> renderables;
		internal static Material viewCubeMat;
		public SelectionPassComponent(Entity parent) : base(parent)//TODO: go back to occlusion query with discard in EditorPickingShader
		{
			ScreenSpace = true;
			CreateNewTemporaryTexture("selectionScene", TextureRole.Color0, 0, 0, TextureFormat.RGBA);//TODO: convert this to GL_R32UI or GL_RB32UI, more than 64 bit is unlikely to be necessary, also optimize size to 1 pixel and rect when multiselecting
			CreateNewTemporaryTexture("selectionDepth", TextureRole.Depth, 0, 0, TextureFormat.DepthFloat);
			Material.BindGlobalProperty("alphaThreshold", 0.33f);
			rendererMask.SetFlag(0);
		}

		public override void Execute()//bepuphysic raycast then render to check if alpha is ok or box against scene but without doublechecking alpha or instead relying on it simply cast origin point to screen and check mouse radius or box
		{
			/*if (readPixels is 0)
			{
				Console.WriteLine(FBO);
				BindFrame();
				
				readPixels = -1;
			}*
			Material.BindGlobalProperty("enablePicking", 1f);//TODO: test against new version and color+bepu box select

			if (SceneView.mouseLocked is false)
			{
				var renderables = new List<Entity>();
				var renderers = Entity.FindAllWithComponentsAndTags(rendererMask, Camera.main.cullingTags, cullTags: true).GetEnumerator();
				int id = (int)Gizmo.UniformScale + 1;
				while (renderers.MoveNext())
					foreach (var r in renderers.Current)
					{
						var renderer = r.GetComponent<MeshRenderer>();
						if (renderer.active is true)
						{
							var uid = MemoryMarshal.CreateReadOnlySpan(ref id, 1).AsBytes();
							renderer.material.BindProperty("colorId", new Color(uid[0], uid[1], uid[2], uid[3]));
							renderables.Add(r);
							id++;
						}
					}
				PreparePass();

				Draw(renderables);
				if (SceneStructureView.tree.SelectedNode is { UserData: Entity })
				{
					MainWindow.backendRenderer.WriteDepth(true);
					MainWindow.backendRenderer.ClearDepth();
					for (int i = (int)Gizmo.TranslateX; i < (int)Gizmo.UniformScale; i++)
					{
						var uid = MemoryMarshal.CreateReadOnlySpan(ref i, 1).AsBytes();
						var gizmo = (Gizmo)i;
						Material material = gizmo switch
						{
							Gizmo.TranslateX => DrawHelper.lineMaterialX,
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
						Material extra = gizmo switch
						{
							Gizmo.TranslateX => DrawHelper.coneMaterialX,
							Gizmo.TranslateY =>
								DrawHelper.coneMaterialY,
							Gizmo.TranslateZ =>
								DrawHelper.coneMaterialZ,
							_ => null
						};
						extra?.BindProperty("colorId", new Color(uid[0], uid[1], uid[2], uid[3]));
						extra?.Draw();
						material?.BindProperty("colorId", new Color(uid[0], uid[1], uid[2], uid[3]));
						material?.Draw();
					}

					//foreach (var selected in SceneStructureView.tree.SelectedNode)
					/*if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
					{
						Manipulators.DrawCombinedGizmos(entity);
					}*
				}
				foreach (var i in ..((int)Gizmo.TranslateX - 1))
				{
					var ind = i;
					var uid = MemoryMarshal.CreateReadOnlySpan(ref ind, 1).AsBytes();
					viewCubeMat.BindProperty("colorId", new Color(uid[0], uid[1], uid[2], uid[3]));
					viewCubeMat.Draw();
				}
				//if (readPixels is 0)
				{

					var pixels = MainWindow.backendRenderer.ReadPixels((int)SceneView.localMousePos.X, (int)SceneView.localMousePos.Y - 1, 1, 1, TextureFormat.RGBA);
					//TODO: optimize using PBO
					var index = Unsafe.ReadUnaligned<int>(ref pixels[0]);
					if (SceneView.locPos.HasValue)
					{
						if (index is not 0)
						{
							var upperLimit = (int)Gizmo.UniformScale + 1;
							if (index < upperLimit)
							{
								Manipulators.SelectedGizmoId = (Gizmo)index;
								Manipulators.hoveredGizmoId = Gizmo.Invalid;
								//Selection.Asset = viewCubeMat;
							}
							else
							{
								Camera.main.pivot = renderables[index - upperLimit];
								Selection.Asset = renderables[index - upperLimit];
							}
							if (index > (int)Gizmo.TranslateX - 1)
								SceneView.mouseLocked = true;
						}
						else
						{
							Selection.Asset = null;
							Camera.main.pivot = null;
						}
						SceneView.locPos = null;
					}
					else if (index is not (int)Gizmo.Invalid)
					{
						if (index is > (int)Gizmo.UniformScale)
						{
							Selection.HoveredObject = renderables[index - ((int)Gizmo.UniformScale + 1)];
							viewCubeMat.BindProperty("hoveredColorId", new Color(0, 0, 0, 0));
							Manipulators.hoveredGizmoId = Gizmo.Invalid;
						}
						else
						{
							Manipulators.hoveredGizmoId = (Gizmo)index;
							viewCubeMat.BindProperty("hoveredColorId", new Color(pixels[0], 0, 0, 255));
							Selection.HoveredObject = null;
						}
					}
					else
					{
						Manipulators.hoveredGizmoId = Gizmo.Invalid;
						Selection.HoveredObject = null;
						viewCubeMat.BindProperty("hoveredColorId", new Color(0, 0, 0, 0));
					}
					//readPixels = 2;
				}
				MainWindow.backendRenderer.ClearBuffer();
				MainWindow.backendRenderer.ClearColor(0f, 0f, 0f, 0f);
			}

			/*
			//if (readPixels is not -1)
			//	readPixels--;*
			Material.BindGlobalProperty("enablePicking", 0f);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
		}

	}
}

/*Material.BindGlobalProperty("enablePicking", 1f);
int width, height;
width = cam.Width;
height = cam.Height;
MainWindow.backendRenderer.Viewport(0, 0, width, height);
MainWindow.backendRenderer.Clip((int)SceneView.localMousePos.X, (int)SceneView.localMousePos.Y, 1, 1);
MainWindow.backendRenderer.BindBuffers(Target.Frame, FBO);
//MainWindow.backendRenderer.ClearBuffer();
GL.Disable(EnableCap.Blend);
//blit from SceneCommand to this framebuffer instead
var renderables = new List<Entity>();
var renderers = Entity.FindAllWithComponentsAndTags(rendererMask, Camera.main.cullingTags, cullTags: true).GetEnumerator();
while (renderers.MoveNext())
	renderables.AddRange(renderers.Current);
if (renderables.Count is 0) return;
if (renderables.Count > ids.Length)
{
	GL.DeleteQueries(ids.Length, ids);
	ids = new uint[renderables.Count];
	GL.GenQueries(ids.Length, ids);
}
GL.ColorMask(false, false, false, false);
GL.Enable(EnableCap.DepthTest);//disable when all objects or when depth peeling to be selected or enabled + less when only top most
//GL.DepthFunc(DepthFunction.Less);
//GL.Disable(EnableCap.CullFace);
foreach (var i in ..ids.Length)
{
	var renderer = renderables[i].GetComponent<MeshRenderer>();
	if (renderer is { active: true })
	{
		GL.BeginQuery(QueryTarget.SamplesPassed, ids[i]);
		renderer.material.BindProperty("colorId", new Color(0, 255, 0, 255));
		renderer.Render();
		GL.EndQuery(QueryTarget.SamplesPassed);
	}
}
uint result;
int index = -1;
foreach (var i in ..ids.Length)
{
	GL.GetQueryObject(ids[i], GetQueryObjectParam.QueryResult, out result);
	if (result is not 0)
	{
		Console.WriteLine(index = i);
		//break;
	}
}
if (SceneView.locPos.HasValue)
{
	if (index is not -1)
	{
		/*if (index is < 39)
		{
			Manipulators.selectedGizmoId = (Gizmo)index;
			Manipulators.hoveredGizmoId = Gizmo.Invalid;
			//Selection.Asset = viewCubeMat;
		}
		else
		{*
		Camera.main.pivot = renderables[index];
		Selection.Asset = renderables[index];
		//}
		//if (index > 26)
		SceneView.mouseLocked = true;
	}
	else
	{
		Selection.Asset = null;
		Camera.main.pivot = null;
	}
	SceneView.locPos = null;
}
else if (index is not -1)
{
	//if (index is > 38)
	{
		Selection.HoveredObject = renderables[index];
		//viewCubeMat.BindProperty("hoveredColorId", new Color(0, 0, 0, 0));
		Manipulators.hoveredGizmoId = Gizmo.Invalid;
	}
	/*else
	{
		Manipulators.hoveredGizmoId = (Gizmo)index;
		//viewCubeMat.BindProperty("hoveredColorId", new Color(pixel[0], 0, 0, 255));
		Selection.HoveredObject = null;
	}*
}
else
{
	Manipulators.hoveredGizmoId = Gizmo.Invalid;
	Selection.HoveredObject = null;
	//viewCubeMat.BindProperty("hoveredColorId", new Color(0, 0, 0, 0));
}
GL.Enable(EnableCap.DepthTest);

//GL.Enable(EnableCap.CullFace);

GL.ColorMask(true, true, true, true);

PreparePass();

List<Entity> selected = new();
List<Entity> hovered = new();

if (Selection.HoveredObject is Entity e)
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
Material.BindGlobalProperty("enablePicking", 0f);*/