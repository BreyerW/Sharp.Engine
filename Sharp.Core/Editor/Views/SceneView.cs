using SharpAsset.AssetPipeline;
using Squid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sharp.Engine.Components;
using SharpAsset;
using Sharp.Core;
using PluginAbstraction;
using Sharp.Physic;
using BepuPhysics.Collidables;

namespace Sharp.Editor.Views
{
	public class SceneView : View
	{
		private static SimpleThreadDispatcher dispatcher = new(4);
		private static BitMask rendererMask = new(0);
		private int cell_size = 32;
		private int grid_size = 4096;
		private static Material highlight;
		private static Material viewCubeMat;
		private static Material boundingBoxMat;
		private static int[] ids = Array.Empty<int>();
		internal static bool leftCtrlPressed = false;
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static List<Renderer> renderers = new List<Renderer>();
		public static Queue<IStartableComponent> startables = new Queue<IStartableComponent>();
		public static bool globalMode = false;
		public static bool snapMode = false;
		public static Vector3 translateSnap = new Vector3(100, 100, 100);
		public static Vector3 scaleSnap = new Vector3(1, 1, 1);
		public static Vector3 rotateSnap = new Vector3(90, 90, 90);
		public static float screenRotateSnap = 90;
		internal static Vector2 localMousePos;
		internal static Vector2 mouseDela;
		internal static Point? locPos = null;
		public static bool mouseLocked = false;
		private bool enableBoundingBoxes;

		static SceneView()
		{
			LoadScene();
			rendererMask = Extension.GetBitMaskFor<Renderer>();
		}
		private static void LoadScene()
		{
			var e = new Entity();
			e.transform.Position = new Vector3(0f, 10f, 0f);
			var angles = e.transform.Rotation * NumericsExtensions.Deg2Rad;
			e.transform.ModelMatrix = Matrix4x4.CreateScale(e.transform.Scale) * Matrix4x4.CreateFromYawPitchRoll(angles.Y, angles.X, angles.Z) * Matrix4x4.CreateTranslation(e.transform.Position);
			var cam = e.AddComponent<Camera>();
			Camera.main = cam;
			e.AddComponent<DepthPrePassComponent>();
			e.AddComponent<HighlightPassComponent>();
			cam.SetModelviewMatrix();
			cam.SetProjectionMatrix();
			e.name = "Main Camera";
			//e.OnTransformChanged += ((sender, evnt) => cam.SetModelviewMatrix());

			cam.active = true;
			var eLight = new Entity();
			eLight.name = "Directional_Light";//TODO: Text renderer bugs out on white space sometimes check it
			eLight.transform.Position = Camera.main.Parent.transform.Position;
			eLight.AddComponent<Light>();

			ref var sh = ref Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\WireframeShader.shader");
			boundingBoxMat = new Material();
			boundingBoxMat.BindShader(0, sh);
			boundingBoxMat.BindProperty("removeDiagonalEdges", 1f);
			var c = Pipeline.Get<SharpAsset.Mesh>().GetAsset("cubeBB");
			boundingBoxMat.BindProperty("mesh", c);

			ref var shader = ref Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\ViewCubeShader.shader");
			ref var viewCubeMesh = ref Pipeline.Get<SharpAsset.Mesh>().Import(Application.projectPath + @"\Content\viewcube_submeshed.dae");
			ref var cubeTexture = ref Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_textured.png");
			ref var mask = ref Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_mask.png");

			viewCubeMat = new Material();

			viewCubeMat.BindShader(0, shader);
			viewCubeMat.BindProperty("CubeTex", cubeTexture);
			viewCubeMat.BindProperty("mask", mask);
			viewCubeMat.BindProperty("edgeColor", new Color(150, 150, 150, 255));
			viewCubeMat.BindProperty("highlightColor", new Color(0.75f, 0.75f, 0.75f, 0.75f));
			viewCubeMat.BindProperty("enableHighlight", 0);
			viewCubeMat.BindProperty("faceColor", new Color(100, 100, 100, 255));//192 as light alternative?
			viewCubeMat.BindProperty("xColor", new Color(255, 75, 75, 255));//Manipulators.xColor
			viewCubeMat.BindProperty("yColor", new Color(100, 255, 100, 255));//Manipulators.yColor
			viewCubeMat.BindProperty("zColor", new Color(80, 150, 255, 255));//Manipulators.zColor
			viewCubeMat.BindProperty("mesh", viewCubeMesh);
			HighlightPassComponent.viewCubeMat = viewCubeMat;
			shader = ref Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightShader.shader");
			ref var screenMesh = ref Pipeline.Get<SharpAsset.Mesh>().GetAsset("screen_space_square");


			highlight = new Material();
			highlight.BindShader(0, shader);
			ref var selectionTexture = ref Pipeline.Get<Texture>().GetAsset("highlightScene");
			highlight.BindProperty("MyTexture", selectionTexture);
			ref var selectionDepthTexture = ref Pipeline.Get<Texture>().GetAsset("highlightDepth");
			highlight.BindProperty("SelectionDepthTex", selectionDepthTexture);
			ref var sceneDepthTexture = ref Pipeline.Get<Texture>().GetAsset("depthTarget");
			highlight.BindProperty("SceneDepthTex", sceneDepthTexture);
			highlight.BindProperty("outline_color", Manipulators.selectedColor);
			highlight.BindProperty("mesh", screenMesh);

		}
		public SceneView(uint attachToWindow) : base(attachToWindow)
		{
			//System.Threading.Tasks.Task.Run(() => );
			//Selection.Repeat(Selection.IsSelectionDirty, 30, 30, CancellationToken.None);
			AllowDrop = true;
			OnDragFinished += Panel_Drop;
			MouseDown += Panel_MouseDown;
			MouseUp += Panel_MouseUp;
			KeyDown += SceneView_KeyDown;
			KeyUp += SceneView_KeyUp;
			SizeChanged += SceneView_SizeChanged;
			Squid.UI.MouseMove += UI_MouseMove;
			Squid.UI.MouseUp += UI_MouseUp;
			Button.Text = "Scene";
			AllowFocus = true;
			ids = new int[(int)Gizmo.UniformScale];
			PluginManager.backendRenderer.GenerateBuffers(Target.OcclusionQuery, ids);
		}

		private void SceneView_KeyUp(Control sender, KeyEventArgs args)
		{
			//Console.WriteLine("keyup");
			if (args.Key == Keys.LEFTCONTROL)
			{
				if (Manipulators.selectedGizmoId is Gizmo.Invalid)
					Manipulators.useUniformScale = false;
				leftCtrlPressed = false;
			}
		}

		private void UI_MouseUp(Control sender, MouseEventArgs args)
		{
			Console.WriteLine("global mouse happened");
			mouseLocked = false;
			Manipulators.Reset();
		}

		private void UI_MouseMove(Control sender, MouseEventArgs args)
		{
			if (mouseLocked)
			{
				if (Manipulators.selectedGizmoId is Gizmo.Invalid)
				{
					Camera.main.Rotate(Squid.UI.MouseDelta.x, Squid.UI.MouseDelta.y, 0.3f);//maybe divide delta by fov?
				}
				else//simple, precise, snapping
				{
					if (Squid.UI.MouseDelta.x != 0 || Squid.UI.MouseDelta.y != 0)
					{
						var orig = Camera.main.Parent.transform.Position;
						//var winPos = Window.windows[attachedToWindow].Position;
						var localMouse = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
						var start = Camera.main.ScreenToWorld(localMouse.x, localMouse.y, Size.x, Size.y + Button.Size.y, 1);

						var ray = new Ray(orig, (start - orig).Normalize());
						//foreach (var selected in SceneStructureView.tree.SelectedChildren)
						if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
						{
							if (Manipulators.selectedGizmoId < Gizmo.RotateX)
							{
								Manipulators.HandleTranslation(entity, ref ray);
							}
							else if (Manipulators.selectedGizmoId is < Gizmo.ScaleX or Gizmo.RotateScreen)
							{
								Manipulators.HandleRotation(entity, ref ray);
							}
							else
							{
								Manipulators.HandleScale(entity, ref ray);
							}
						}
					}
				}
			}
			localMousePos = new Vector2(Squid.UI.MousePosition.x - Location.x, Size.y - (Squid.UI.MousePosition.y - Location.y));

			highlight.BindProperty("mousePos", localMousePos);
			mouseDela = new Vector2(Squid.UI.MouseDelta.x, Squid.UI.MouseDelta.y);
		}

		private void SceneView_SizeChanged(Control sender)
		{
			Camera.main.AspectRatio = (float)Size.x / (Size.y);

			Camera.main.SetOrthoMatrix(Size.x, Size.y);
			Camera.main.Width = Size.x;
			Camera.main.Height = Size.y;
			Camera.main.SetProjectionMatrix();

			Material.BindGlobalProperty("viewPort", new Vector2(Size.x, Size.y));
		}

		private void SceneView_KeyDown(Control sender, KeyEventArgs args)
		{
			if (Camera.main.moved)
			{
				Camera.main.moved = false;
				Camera.main.SetModelviewMatrix();
			}
			if (args.Key == Keys.LEFTCONTROL)
				leftCtrlPressed = true;
			if (args.Key == Keys.Q)
				Camera.main.Move(Vector3.UnitY, 1f, 0f);
			if (args.Key == Keys.E)
				Camera.main.Move(Vector3.UnitY, -1f, 0f);
			if (args.Key == Keys.A)
				Camera.main.Move(Vector3.UnitX, -1f, 0f);
			if (args.Key == Keys.D)
				Camera.main.Move(Vector3.UnitX, 1f, 0f);
			if (args.Key == Keys.W)
				Camera.main.Move(Vector3.UnitZ, -1f, 0f);
			if (args.Key == Keys.S)
				Camera.main.Move(Vector3.UnitZ, 1f, 0f);
			if (args.Key == Keys.B)
				enableBoundingBoxes = !enableBoundingBoxes;
		}

		private void Panel_MouseUp(Control sender, MouseEventArgs args)
		{
			if (Manipulators.selectedGizmoId is not Gizmo.Invalid and < Gizmo.TranslateX)
				Manipulators.HandleViewCube(SceneStructureView.tree.SelectedNode?.UserData as Entity);
			mouseLocked = false;
			Manipulators.selectedGizmoId = Gizmo.Invalid;
		}

		private void Panel_MouseDown(Control sender, MouseEventArgs args)
		{
			if (args.Button is 1)
			{
				mouseLocked = true;
				Manipulators.selectedGizmoId = Gizmo.Invalid;
			}
			else if (args.Button is 0)
			{
				mouseLocked = false;
				locPos = new Point(Squid.UI.MousePosition.x - Location.x, Size.y - (Squid.UI.MousePosition.y - Location.y));
				//if (Manipulators.selectedAxisId < 7)
				{
					//	Manipulators.HandleRotation(entity, ref ray);
				}
			}
		}

		private void Panel_Drop(Control sender, DragDropEventArgs e)
		{
			if (e.Source.UserData is ValueTuple<string, string>[] entities)
				foreach (var asset in entities)
				{
					(string name, string extension) = asset;
					var pipeline = Pipeline.Get(extension);
					pipeline.ApplyIAsset(pipeline.ImportIAsset(name), this);
				}
		}

		protected override void DrawAfter()
		{

			//if (!IsVisible) return;
			//base.Render();
			//if (Camera.main is null) return;
			while (startables.Count != 0)
				startables.Dequeue().Start();
			//SelectionPassComponent.clip = (Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			var angles = Camera.main.Parent.transform.Rotation * NumericsExtensions.Deg2Rad;
			var rotationMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(-angles.X) * Matrix4x4.CreateRotationZ(angles.Z);

			var m = Matrix4x4.CreateScale(40) * rotationMatrix * Matrix4x4.CreateTranslation(Size.x - 70, 70, 0) * Matrix4x4.CreateOrthographicOffCenter(0, Camera.main.Width, Camera.main.Height, 0, -100f, 100f);

			viewCubeMat.BindProperty("model", m);

			var renderables = new List<Renderer>();
			var transparentRenderables = new List<Renderer>();
			var tester = new BroadPhaseCallback();
			CollisionDetection.simulation.BroadPhase.FrustumSweep(Camera.main.Parent.transform.ModelMatrix * Camera.main.ProjectionMatrix, ref tester, dispatcher: dispatcher);

			foreach (var i in ..CollisionDetection.inFrustumLength)
			{
				var entity = CollisionDetection.inFrustum[i].GetInstanceObject<Entity>();
				if (entity.ComponentsMask.HasAnyFlags(rendererMask) && entity.TagsMask.HasNoFlags(Camera.main.cullingTags))
				{
					var renderer = entity.GetComponent<MeshRenderer>();
					if (renderer.material.IsBlendRequiredForPass(0))
						transparentRenderables.Add(renderer);
					else
						renderables.Add(renderer);
				}
			}

			transparentRenderables.Sort(new OrderByDistanceToCamera());

			if (renderables.Count + transparentRenderables.Count + (int)Gizmo.UniformScale > ids.Length || renderables.Count + transparentRenderables.Count + (int)Gizmo.UniformScale < ids.Length)
			{
				if (ids is not null)
					PluginManager.backendRenderer.DeleteBuffers(Target.OcclusionQuery, ids);
				ids = new int[renderables.Count + transparentRenderables.Count + (int)Gizmo.UniformScale];
				PluginManager.backendRenderer.GenerateBuffers(Target.OcclusionQuery, ids);
			}
			var commandBuffers = Camera.main.Parent.GetAllComponents<CommandBufferComponent>();
			foreach (var command in commandBuffers)
				command.Execute();

			PluginManager.backendRenderer.BindBuffers(Target.Frame, 0);
			PluginManager.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			PluginManager.backendRenderer.Clip(Squid.UI.MousePosition.x - 1, Canvas.Size.y - Squid.UI.MousePosition.y - 1, 1, 1);//TODO: 3x3 or more for rubber band style?

			PluginManager.backendRenderer.ClearBuffer();

			PluginManager.backendRenderer.DisableState(RenderState.Blend);
			if (mouseLocked is false)
			{

				PluginManager.backendRenderer.SetColorMask(false, false, false, false);

				PluginManager.backendRenderer.EnableState(RenderState.DepthTest);//disable when all objects or when depth peeling to be selected or enabled + less when only top most
																				 //PluginManager.backendRenderer.DisableState(RenderState.DepthMask);
				if (SceneStructureView.tree.SelectedNode is { UserData: Entity })
				{
					DrawHelper.gizmoMaterial.Draw();
					DrawHelper.screenGizmoMaterial.Draw();
				}
				viewCubeMat.Draw();

				PluginManager.backendRenderer.SetDepthFunc(DepthFunc.Equal);

				if (SceneStructureView.tree.SelectedNode is { UserData: Entity })
				{
					foreach (var i in ..(Gizmo.UniformScale - Gizmo.TranslateX - 1))
					{
						using (PluginManager.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateX - 1 + i]))
							DrawHelper.gizmoMaterial.Draw(subMesh: i);
					}
					using (PluginManager.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.RotateScreen - 1]))
					{
						DrawHelper.screenGizmoMaterial.Draw();
					}
				}

				foreach (var i in ..((int)Gizmo.TranslateX - 1))
				{
					using (PluginManager.backendRenderer.StartQuery(Target.OcclusionQuery, ids[i]))
						viewCubeMat.Draw(subMesh: i);
				}
				//PluginManager.backendRenderer.Clip(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
				PluginManager.backendRenderer.SetDepthFunc(DepthFunc.Less);
				//PluginManager.backendRenderer.ClearDepth();
				foreach (var renderable in renderables)
				{
					if (renderable is { active: true })
					{
						renderable.Render();
					}
				}

				foreach (var renderable in transparentRenderables)
				{
					if (renderable is { active: true })
					{
						renderable.Render();
					}
				}
				//PluginManager.backendRenderer.Clip(Squid.UI.MousePosition.x - 1, Canvas.Size.y - Squid.UI.MousePosition.y - 1, 1, 1);//TODO: 3x3 or more for rubber band style?

				//PluginManager.backendRenderer.EnableState(RenderState.DepthTest);
				PluginManager.backendRenderer.SetDepthFunc(DepthFunc.Equal);//or Gequal when want square selection including hidden objects, can leave as is if only top most objects should pass
				Material.BindGlobalProperty("enablePicking", 1f);
				var queryOffset = (int)Gizmo.UniformScale;
				foreach (var i in ..renderables.Count)
				{
					var renderer = renderables[i];
					if (renderer is { active: true })//TODO: sort front to back
					{
						using (PluginManager.backendRenderer.StartQuery(Target.OcclusionQuery, ids[i + queryOffset]))
							renderer.Render();
					}
				}
				queryOffset += renderables.Count;
				foreach (var i in ..transparentRenderables.Count)//TODO: can temporarily replace shader to simpler one since correct depth is already filled in previous step
				{
					var renderer = transparentRenderables[i];
					if (renderer is { active: true })
					{
						using (PluginManager.backendRenderer.StartQuery(Target.OcclusionQuery, ids[i + queryOffset]))
							renderer.Render();
					}
				}
			}
			Material.BindGlobalProperty("enablePicking", 0f);
			PluginManager.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			PluginManager.backendRenderer.Clip(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			PluginManager.backendRenderer.SetColorMask(true, true, true, true);

			PluginManager.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			PluginManager.backendRenderer.ClearBuffer();
			PluginManager.backendRenderer.SetDepthFunc(DepthFunc.Less);

			//blit from SceneCommand to this framebuffer instead
			PluginManager.backendRenderer.EnableState(RenderState.DepthMask);
			foreach (var renderable in renderables)
			{
				if (renderable is { active: true })
				{
					renderable.Render();
				}
			}

			PluginManager.backendRenderer.DisableState(RenderState.DepthMask);
			PluginManager.backendRenderer.EnableState(RenderState.Blend);

			foreach (var renderable in transparentRenderables)
			{
				if (renderable is { active: true })
				{
					renderable.Render();
				}
			}
			if (enableBoundingBoxes)
				foreach (var pos in CollisionDetection.frozenMapping)
				{
					boundingBoxMat.BindProperty("model", pos.Value.mat);
					boundingBoxMat.Draw();
				}
			DrawHelper.DrawGrid(Camera.main.Parent.transform.Position);
			PluginManager.backendRenderer.EnableState(RenderState.DepthMask);

			highlight.Draw();

			PluginManager.backendRenderer.ClearDepth();
			PluginManager.backendRenderer.EnableState(RenderState.DepthMask);
			if (SceneStructureView.tree.SelectedNode?.UserData is Entity e)
			{
				//foreach (var selected in SceneStructureView.tree.SelectedChildren)
				{
					Manipulators.DrawCombinedGizmos(e);
				}
			}

			if (Manipulators.hoveredGizmoId is < Gizmo.TranslateX and not Gizmo.Invalid)
			{

				if (Manipulators.hoveredGizmoId is not Gizmo.Invalid + 1)
					viewCubeMat.Draw(..(int)(Manipulators.hoveredGizmoId - 1));

				viewCubeMat.BindProperty("enableHighlight", 1);
				viewCubeMat.Draw((int)Manipulators.hoveredGizmoId - 1);
				viewCubeMat.BindProperty("enableHighlight", 0);

				if (Manipulators.hoveredGizmoId is not Gizmo.TranslateX - 1)
					viewCubeMat.Draw((int)Manipulators.hoveredGizmoId..(int)(Gizmo.TranslateX - 1));

			}
			else
				viewCubeMat.Draw();

			var index = -1;
			int result;
			bool editorObjPicked = false;
			bool opaqueObjPicked = false;
			bool transparentObjPicked = false;

			foreach (var i in ..ids.Length)//TODO: move to the end of rendering to minimize stall?
			{
				PluginManager.backendRenderer.GetQueryResult(ids[i], out result);
				if (result is not 0)
				{
					if (i + 1 < (int)Gizmo.UniformScale)
					{
						index = i + 1;
						editorObjPicked = true;
					}
					else if (i < (int)Gizmo.UniformScale + renderables.Count)
					{
						index = i - (int)Gizmo.UniformScale;
						opaqueObjPicked = true;
					}
					else
					{
						index = i - (int)Gizmo.UniformScale - renderables.Count;
						transparentObjPicked = true;
					}
					break;
				}
			}
			if (locPos.HasValue)
			{
				if (index is not -1)
				{
					if (editorObjPicked)
					{
						if (leftCtrlPressed && index is > (int)Gizmo.RotateZ)
							Manipulators.useUniformScale = true;
						Manipulators.selectedGizmoId = (Gizmo)index;
					}
					else if (opaqueObjPicked)
					{
						Camera.main.pivot = renderables[index].Parent;
						Selection.Asset = renderables[index].Parent;
					}
					else
					{
						Camera.main.pivot = transparentRenderables[index].Parent;
						Selection.Asset = transparentRenderables[index].Parent;
					}
					if (editorObjPicked && index > (int)Gizmo.TranslateX - 1)
						mouseLocked = true;
				}
				else
				{
					Selection.Asset = null;
					Camera.main.pivot = null;
				}
				locPos = null;
			}
			else if (index is not -1)
			{
				if (opaqueObjPicked)
				{
					Selection.HoveredObject = renderables[index].Parent;
					Manipulators.hoveredGizmoId = Gizmo.Invalid;
				}
				else if (transparentObjPicked)
				{
					Selection.HoveredObject = transparentRenderables[index].Parent;
					Manipulators.hoveredGizmoId = Gizmo.Invalid;
				}
				else
				{
					if (leftCtrlPressed && index is > (int)Gizmo.RotateZ)
						Manipulators.useUniformScale = true;
					Manipulators.hoveredGizmoId = (Gizmo)index;
					Selection.HoveredObject = null;
				}
			}
			else
			{
				Manipulators.hoveredGizmoId = Gizmo.Invalid;
				Selection.HoveredObject = null;
			}
			PluginManager.backendRenderer.Viewport(0, 0, Canvas.Size.x, Canvas.Size.y);
			CollisionDetection.inFrustumLength = 0;
		}

	}

	internal class OrderByDistanceToCamera : IComparer<Renderer>
	{
		public int Compare(Renderer x, Renderer y)
		{
			var camPos = Camera.main.Parent.transform.Position;
			var xDistance = (x.Parent.transform.Position - camPos).Length();
			var yDistance = (y.Parent.transform.Position - camPos).Length();
			if (xDistance > yDistance) return 1;
			else if (xDistance == yDistance) return 0;
			return -1;
		}
	}
}