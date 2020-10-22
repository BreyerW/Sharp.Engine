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

namespace Sharp.Editor.Views
{
	public class SceneView : View
	{
		private static Bitask rendererMask = new(0);
		private int cell_size = 32;
		private int grid_size = 4096;
		private static Material highlight;
		private static Material editorHighlight;
		private static Material viewCubeMat;
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static List<Renderer> renderers = new List<Renderer>();
		public static Queue<IStartableComponent> startables = new Queue<IStartableComponent>();
		public static bool globalMode = false;
		internal static Vector2 localMousePos;
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
			var command = e.AddComponent<DepthPrePassComponent>() as CommandBufferComponent;
			command.ScreenSpace = true;
			e.AddComponent<EditorSelectionPassComponent>();
			e.AddComponent<SelectionPassComponent>();
			command = e.AddComponent<OutlinePassComponent>();
			command.ScreenSpace = true;
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
			var viewCubeMesh = (Mesh)Pipeline.Get<Mesh>().Import(Application.projectPath + @"\Content\viewcube.dae");
			var viewCubeTexture = (Texture)Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\Cube.png");
			var cubeTexture = (Texture)Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_textured.png");
			var mask = (Texture)Pipeline.Get<Texture>().Import(Application.projectPath + @"\Content\viewcube_mask.png");



			viewCubeMat = new Material();

			viewCubeMat.Shader = shader;
			viewCubeMat.BindProperty("hoverOverColor", Color.Transparent);
			viewCubeMat.BindProperty("MyTexture", viewCubeTexture);
			viewCubeMat.BindProperty("CubeTex", cubeTexture);
			viewCubeMat.BindProperty("mask", mask);
			viewCubeMat.BindProperty("edgeColor", new Color(150, 150, 150, 255));
			viewCubeMat.BindProperty("faceColor", new Color(100, 100, 100, 255));//192 as light alternative?
			viewCubeMat.BindProperty("xColor", Manipulators.xColor);
			viewCubeMat.BindProperty("yColor", Manipulators.yColor);
			viewCubeMat.BindProperty("zColor", Manipulators.zColor);
			viewCubeMat.BindProperty("mesh", viewCubeMesh);
			EditorSelectionPassComponent.viewCubeMat = viewCubeMat;
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightShader.shader");
			ref var screenMesh = ref Pipeline.Get<Mesh>().GetAsset("screen_space_square");


			highlight = new Material();
			highlight.Shader = shader;
			ref var selectionTexture = ref Pipeline.Get<Texture>().GetAsset("selectionTarget");
			highlight.BindProperty("MyTexture", selectionTexture);
			ref var selectionDepthTexture = ref Pipeline.Get<Texture>().GetAsset("selectionDepthTarget");
			highlight.BindProperty("SelectionDepthTex", selectionDepthTexture);
			ref var sceneDepthTexture = ref Pipeline.Get<Texture>().GetAsset("depthTarget");
			highlight.BindProperty("SceneDepthTex", sceneDepthTexture);
			highlight.BindProperty("outline_color", Manipulators.selectedColor);
			highlight.BindProperty("mesh", screenMesh);

			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\EditorHighlightShader.shader");

			editorHighlight = new Material();
			editorHighlight.Shader = shader;
			ref var editorSelectionTexture = ref Pipeline.Get<Texture>().GetAsset("editorSelectionScene");
			editorHighlight.BindProperty("MyTexture", editorSelectionTexture);
			editorHighlight.BindProperty("outline_color", new Color(255, 255, 255, 255));
			editorHighlight.BindProperty("mesh", screenMesh);
			EditorSelectionPassComponent.editorHighlight = editorHighlight;
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
		}

		private void SceneView_KeyUp(Control sender, KeyEventArgs args)
		{
			//Console.WriteLine("keyup");
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
							else if (Manipulators.selectedGizmoId < Gizmo.ScaleX)
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
			oldX = Squid.UI.MousePosition.x;
			oldY = Squid.UI.MousePosition.y;
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

			var angles = Camera.main.Parent.transform.Rotation * NumericsExtensions.Deg2Rad;
			var rotationMatrix = Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationX(-angles.X) * Matrix4x4.CreateRotationZ(angles.Z);

			viewCubeMat.BindProperty("model", Matrix4x4.CreateScale(50) * rotationMatrix * Matrix4x4.CreateTranslation(100, 100, 0) * Matrix4x4.CreateOrthographicOffCenter(0, Camera.main.Width, Camera.main.Height, 0, -100f, 100f));

			var commandBuffers = Camera.main.Parent.GetAllComponents<CommandBufferComponent>();
			foreach (var command in commandBuffers)
				command.Execute();

			MainWindow.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.Clip(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.BindBuffers(Target.Frame, 0);

			MainWindow.backendRenderer.SetStandardState();
			MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			MainWindow.backendRenderer.ClearBuffer();
			GL.Enable(EnableCap.Blend);
			//blit from SceneCommand to this framebuffer instead
			var renderables = Entity.FindAllWithComponentsAndTags(rendererMask, Camera.main.cullingTags, cullTags: true).GetEnumerator();
			while (renderables.MoveNext())
				foreach (var renderable in renderables.Current)
				{
					var renderer = renderable.GetComponent<Renderer>();
					if (renderer is { active: true })
						renderer.Render();
				}

			DrawHelper.DrawGrid(Camera.main.Parent.transform.Position);

			//MainWindow.backendRenderer.WriteDepth(false);


			highlight.SendData();

			MainWindow.backendRenderer.WriteDepth(true);
			MainWindow.backendRenderer.ClearDepth();
			if (SceneStructureView.tree.SelectedNode?.UserData is Entity e)
			{
				//foreach (var selected in SceneStructureView.tree.SelectedChildren)
				{
					Manipulators.DrawCombinedGizmos(e);
				}
			}

			viewCubeMat.SendData();
			GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);
			editorHighlight.SendData();
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Enable(EnableCap.Blend);
			MainWindow.backendRenderer.Viewport(0, 0, Canvas.Size.x, Canvas.Size.y);
		}
		private int oldX;
		private int oldY;
	}

	internal class OrderByDistanceToCamera : IComparer<Vector3>
	{
		public int Compare(Vector3 x, Vector3 y)
		{
			var xDistance = (x - Camera.main.Parent.transform.Position).Length();
			var yDistance = (y - Camera.main.Parent.transform.Position).Length();
			if (xDistance > yDistance) return 1;
			else if (xDistance < yDistance) return -1;
			else return 0;
		}
	}
}