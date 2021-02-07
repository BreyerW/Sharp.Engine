//using PhysX;
using SharpAsset.Pipeline;
using Squid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sharp.Engine.Components;
using SharpAsset;
using SharpSL;
using OpenTK.Graphics.OpenGL;
using System.Runtime.CompilerServices;
using Sharp.Core;
using System.Runtime.InteropServices;
using Microsoft.Toolkit.HighPerformance.Extensions;
using System.Reflection;

namespace Sharp.Editor.Views
{
	public class SceneView : View
	{
		private static BitMask rendererMask = new(0);
		private int cell_size = 32;
		private int grid_size = 4096;
		private static Material highlight;
		private static Material viewCubeMat;
		private static int[] ids = Array.Empty<int>();
		internal static bool leftCtrlPressed = false;
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static List<Renderer> renderers = new List<Renderer>();
		public static Queue<IStartableComponent> startables = new Queue<IStartableComponent>();
		public static bool globalMode = false;
		internal static Vector2 localMousePos;
		internal static Vector2 mouseDela;
		internal static Point? locPos = null;
		public static bool mouseLocked = false;
		/*DebugProc DebugCallbackInstance = DebugCallback;

		static void DebugCallback(DebugSource source, DebugType type, int id,
			DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string msg = Marshal.PtrToStringAnsi(message);
			Console.WriteLine("[GL] {0}; {1}; {2}; {3}; {4};",
				source, type, id, severity, msg);
		}*/
		static SceneView()
		{
			LoadScene();
			rendererMask.SetFlag(0);
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

			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\ViewCubeShader.shader");
			var viewCubeMesh = (Mesh)Pipeline.Get<Mesh>().Import(Application.projectPath + @"\Content\viewcube_submeshed.dae");
			var cubeTexture = (Texture)Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_textured.png");
			var mask = (Texture)Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_mask.png");



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
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightShader.shader");
			ref var screenMesh = ref Pipeline.Get<Mesh>().GetAsset("screen_space_square");


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
			MainWindow.backendRenderer.GenerateBuffers(Target.OcclusionQuery, ids);
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
							else if (Manipulators.selectedGizmoId is < Gizmo.ScaleX)
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

			Camera.main.frustum = new Frustum(Camera.main.ViewMatrix * Camera.main.ProjectionMatrix);
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
			var locPos = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
			var orig = Camera.main.Parent.transform.Position;
			if (e.Source.UserData is ValueTuple<string, string>[] entities)
				foreach (var asset in entities)
				{
					(string name, string extension) = asset;
					Pipeline.Get(extension).Import(name).PlaceIntoScene(null, orig + (Camera.main.ScreenToWorld(locPos.x, locPos.y, Size.x, Size.y) - orig).Normalize() * Camera.main.ZFar * 0.1f);
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

			var renderables = new List<Entity>();
			var rs = Entity.FindAllWithComponentsAndTags(rendererMask, Camera.main.cullingTags, cullTags: true);
			var renderers = rs.GetEnumerator();
			while (renderers.MoveNext())
				renderables.AddRange(renderers.Current);
			//renderables.Sort(new OrderByDistanceToCamera());
			if (renderables.Count + (int)Gizmo.UniformScale > ids.Length || renderables.Count + (int)Gizmo.UniformScale < ids.Length)
			{
				if (ids is not null)
					MainWindow.backendRenderer.DeleteBuffers(Target.OcclusionQuery, ids);
				ids = new int[renderables.Count + (int)Gizmo.UniformScale];
				MainWindow.backendRenderer.GenerateBuffers(Target.OcclusionQuery, ids);
			}
			var commandBuffers = Camera.main.Parent.GetAllComponents<CommandBufferComponent>();
			foreach (var command in commandBuffers)
				command.Execute();

			MainWindow.backendRenderer.BindBuffers(Target.Frame, 0);
			MainWindow.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.Clip(Squid.UI.MousePosition.x, Canvas.Size.y - Squid.UI.MousePosition.y, 1, 1);//TODO: 3x3 or more for rubber band style?

			//MainWindow.backendRenderer.SetStandardState();
			//MainWindow.backendRenderer.ClearColor(0f, 0f, 0f, 0f);
			MainWindow.backendRenderer.ClearBuffer();

			GL.Disable(EnableCap.Blend);
			//blit from SceneCommand to this framebuffer instead
			//MainWindow.backendRenderer.WriteDepth(false);
			if (mouseLocked is false)
			{
				Material.BindGlobalProperty("enablePicking", 1f);

				MainWindow.backendRenderer.SetColorMask(false, false, false, false);
				GL.Enable(EnableCap.DepthTest);//disable when all objects or when depth peeling to be selected or enabled + less when only top most

				if (SceneStructureView.tree.SelectedNode is { UserData: Entity })
				{
					DrawHelper.gizmoMaterial.Draw();
				}
				viewCubeMat.Draw();

				GL.DepthFunc(DepthFunction.Equal);

				if (SceneStructureView.tree.SelectedNode is { UserData: Entity })
				{
					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateX - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 0);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateY - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 1);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateZ - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 2);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateXY - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 3);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateYZ - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 4);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.TranslateZX - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 5);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.RotateX - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 6);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.RotateY - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 7);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.RotateZ - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 8);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.ScaleX - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 9);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.ScaleY - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 10);

					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[(int)Gizmo.ScaleZ - 1]))
						DrawHelper.gizmoMaterial.Draw(subMesh: 11);
				}
				foreach (var i in ..((int)Gizmo.TranslateX - 1))
				{
					using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[i]))
						viewCubeMat.Draw(subMesh: i);
				}

				MainWindow.backendRenderer.ClearDepth();
				GL.DepthFunc(DepthFunction.Less);
				foreach (var renderable in renderables)
				{
					var renderer = renderable.GetComponent<MeshRenderer>();
					if (renderer is { active: true })//TODO: sort front to back
					{
						renderer.Render();
					}
				}

				GL.Enable(EnableCap.DepthTest);//disable when all objects or when depth peeling to be selected or enabled + less when only top most
				GL.DepthFunc(DepthFunction.Equal);
				//GL.Enable(EnableCap.CullFace);
				foreach (var i in (int)Gizmo.UniformScale..ids.Length)
				{
					var renderer = renderables[i - (int)Gizmo.UniformScale].GetComponent<MeshRenderer>();
					if (renderer is { active: true })//TODO: sort front to back
					{
						using (MainWindow.backendRenderer.StartQuery(Target.OcclusionQuery, ids[i]))
							renderer.Render();
					}
				}
				Material.BindGlobalProperty("enablePicking", 0f);
				//MainWindow.backendRenderer.WriteDepth(true);
			}

			MainWindow.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.Clip(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.SetColorMask(true, true, true, true);

			MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			MainWindow.backendRenderer.ClearBuffer();
			GL.DepthFunc(DepthFunction.Less);
			//GL.Disable(EnableCap.CullFace);
			//GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Blend);
			//blit from SceneCommand to this framebuffer instead
			MainWindow.backendRenderer.WriteDepth(true);
			renderers = rs.GetEnumerator();
			//var level = 0;
			while (renderers.MoveNext())
				foreach (var renderable in renderers.Current)
				{
					var renderer = renderable.GetComponent<MeshRenderer>();
					if (renderer is { active: true })
					{
						//renderer.material.BindProperty("darkening", level++);
						renderer.Render();
					}
				}
			//MainWindow.backendRenderer.WriteDepth(false);
			DrawHelper.DrawGrid(Camera.main.Parent.transform.Position);

			//MainWindow.backendRenderer.ClearDepth();
			//MainWindow.backendRenderer.WriteDepth(false);

			highlight.Draw();

			//if (viewCubeMat.TryGetProperty("isHovered", out float isHovered) && isHovered is 0f)
			{
				//GL.CullFace(CullFaceMode.FrontAndBack);
				//MainWindow.backendRenderer.WriteDepth(false);
			}

			MainWindow.backendRenderer.ClearDepth();
			MainWindow.backendRenderer.WriteDepth(true);
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
			foreach (var i in ..ids.Length)//TODO: move to the end of rendering to minimize stall?
			{
				MainWindow.backendRenderer.GetQueryResult(ids[i], out result);
				if (result is not 0)
				{
					index = i + 1;
					break;
				}
			}
			if (locPos.HasValue)
			{
				if (index is not -1)
				{
					if (index < (int)Gizmo.UniformScale)
					{
						if (leftCtrlPressed && index is > (int)Gizmo.RotateZ)
							Manipulators.useUniformScale = true;
						Manipulators.selectedGizmoId = (Gizmo)index;
					}
					else
					{
						Camera.main.pivot = renderables[index - (int)Gizmo.UniformScale - 1];
						Selection.Asset = renderables[index - (int)Gizmo.UniformScale - 1];
					}
					if (index > (int)Gizmo.TranslateX - 1)
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
				if (index > (int)Gizmo.ScaleZ)
				{
					Selection.HoveredObject = renderables[index - (int)Gizmo.UniformScale - 1];
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
			MainWindow.backendRenderer.Viewport(0, 0, Canvas.Size.x, Canvas.Size.y);
		}
	}

	internal class OrderByDistanceToCamera : IComparer<Entity>
	{
		public int Compare(Entity x, Entity y)
		{
			var xDistance = (x.transform.Position - Camera.main.Parent.transform.Position).Length();
			var yDistance = (y.transform.Position - Camera.main.Parent.transform.Position).Length();
			if (xDistance > yDistance) return 1;
			return -1;
			//return 0;
		}
	}
}