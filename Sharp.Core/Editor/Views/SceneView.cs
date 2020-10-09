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
		private static BitMask rendererMask = new BitMask(0);
		private int cell_size = 32;
		private int grid_size = 4096;
		private static Material highlight;
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static List<Renderer> renderers = new List<Renderer>();
		public static Queue<IStartableComponent> startables = new Queue<IStartableComponent>();
		public static bool globalMode = false;

		private static Point? locPos = null;
		private Vector3 normalizedMoveDir = Vector3.Zero;
		public bool mouseLocked = false;
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
			var command = e.AddComponent<SceneCommandComponent>() as CommandBufferComponent;
			command.ScreenSpace = true;
			command = e.AddComponent<SelectionCommandComponent>();
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

			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\HighlightShader.shader");
			ref var screenMesh = ref Pipeline.Get<Mesh>().GetAsset("screen_space_square");
			ref var selectionTexture = ref Pipeline.Get<Texture>().GetAsset("selectionTarget");
			ref var sceneDepthTexture = ref Pipeline.Get<Texture>().GetAsset("depthTarget");
			ref var selectionDepthTexture = ref Pipeline.Get<Texture>().GetAsset("selectionDepthTarget");
			highlight = new Material();
			highlight.Shader = shader;
			highlight.BindProperty("MyTexture", selectionTexture);
			highlight.BindProperty("SelectionDepthTex", selectionDepthTexture);
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


		}

		private void SceneView_KeyUp(Control sender, KeyEventArgs args)
		{
			//Console.WriteLine("keyup");
		}

		private void UI_MouseUp(Control sender, MouseEventArgs args)
		{
			Console.WriteLine("global mouse happened");
			mouseLocked = false;
		}

		private void UI_MouseMove(Control sender, MouseEventArgs args)
		{
			if (mouseLocked)
			{
				if (Manipulators.selectedAxisId == 0)
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
							if (Manipulators.selectedAxisId < 4)
							{
								Manipulators.HandleTranslation(entity, ref ray);
							}
							else if (Manipulators.selectedAxisId < 7)
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

		//ErrorOutput errorOutput = new ErrorOutput();

		//Foundation foundation = new Foundation(errorOutput);
		//physEngine= new Physics(foundation, checkRuntimeFiles: true);
		//var sceneDesc = new SceneDesc (){Gravity = new System.Numerics.Vector3(0, -9.81f, 0) };
		//physScene = physEngine.CreateScene ();
		private void Panel_MouseUp(Control sender, MouseEventArgs args)
		{
			Manipulators.Reset();
		}

		private void Panel_MouseDown(Control sender, MouseEventArgs args)
		{
			if (args.Button is 1)
			{
				mouseLocked = true;
				Manipulators.selectedAxisId = 0;
			}
			else if (args.Button is 0)
			{
				mouseLocked = false;
				locPos = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
				//if (Manipulators.selectedAxisId < 7)
				{
					//	Manipulators.HandleRotation(entity, ref ray);
				}
			}
		}

		private void Panel_Drop(Control sender, DragDropEventArgs e)
		{
			var locPos = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
			Camera.main.SetModelviewMatrix();
			var orig = Camera.main.Parent.transform.Position;
			if (e.Source.UserData is ValueTuple<string, string>[] entities)
				foreach (var asset in entities)
				{
					(string name, string extension) = asset;
					Pipeline.GetPipeline(extension).Import(name).PlaceIntoScene(null, orig + (Camera.main.ScreenToWorld(locPos.x, locPos.y, Size.x, Size.y) - orig).Normalize() * Camera.main.ZFar * 0.1f);
				}
		}
		protected override void DrawAfter()
		{
			//if (!IsVisible) return;
			//base.Render();
			//if (Camera.main is null) return;
			while (startables.Count != 0)
				startables.Dequeue().Start();

			var selection = Camera.main.Parent.GetComponent<SelectionCommandComponent>();
			selection.Execute();

			var scene = Camera.main.Parent.GetComponent<SceneCommandComponent>();
			scene.Execute();


			MainWindow.backendRenderer.Viewport(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.Clip(Location.x, Canvas.Size.y - (Location.y + Size.y), Size.x, Size.y);
			MainWindow.backendRenderer.BindBuffers(Target.Frame, 0);

			MainWindow.backendRenderer.SetStandardState();
			MainWindow.backendRenderer.ClearColor(0.15f, 0.15f, 0.15f, 1f);
			MainWindow.backendRenderer.ClearBuffer();
			if (locPos.HasValue)
			{
				if (!PickTestForGizmo())
					PickTestForObject();
				locPos = null;
			}
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
			MainWindow.backendRenderer.WriteDepth(false);

			highlight.SendData();


			if (SceneStructureView.tree.SelectedNode?.UserData is Entity e)
			{
				//foreach (var selected in SceneStructureView.tree.SelectedChildren)
				{
					MainWindow.backendRenderer.WriteDepth(true);
					MainWindow.backendRenderer.ClearDepth();

					Manipulators.DrawCombinedGizmos(e, new Vector2(Size.x, Size.y));
					MainWindow.backendRenderer.WriteDepth(false);
				}
			}

			MainWindow.backendRenderer.Viewport(0, 0, Canvas.Size.x, Canvas.Size.y);
		}

		private bool PickTestForGizmo()
		{
			//MainWindow.backendRenderer.ClearBuffer();
			MainWindow.backendRenderer.SetFlatColorState();
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

				//foreach (var selected in SceneStructureView.tree.SelectedNode)
				if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
				{
					Manipulators.DrawCombinedGizmos(entity, new Vector2(Size.x, Size.y), xColor, yColor, zColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);
				}
				if (locPos.HasValue)
				{
					var pixel = MainWindow.backendRenderer.ReadPixels(Squid.UI.MousePosition.x, Canvas.Size.y - Squid.UI.MousePosition.y - 1 /*locPos.Value.y - 64*/, 1, 1);
					int index = ((pixel[0]) << 00) + ((pixel[1]) << 08) + (((pixel[2]) << 16));
					Console.WriteLine("encoded index=" + index);
					if (index > 0 && index < 10)
					{
						Manipulators.selectedAxisId = index;
						return mouseLocked = true;
					}
					else
						Manipulators.selectedAxisId = 0;
				}
			}
			return false;
		}

		private void PickTestForObject()
		{
			var orig = Camera.main.Parent.transform.Position;
			var end = Camera.main.ScreenToWorld(locPos.Value.x, locPos.Value.y, Size.x, Size.y);
			var ray = new Ray(orig, (end - orig).Normalize());
			var hitList = new SortedList<Vector3, Guid>(new OrderByDistanceToCamera());
			foreach (var ent in Root.root)
			{
				var render = ent.GetComponent<MeshRenderer>();
				Vector3 hitPoint = Vector3.Zero;
				if (render != null && render.material.TryGetProperty("mesh", out Mesh Mesh) && Mesh.bounds.Intersect(ray, ent.transform.ModelMatrix, out hitPoint))
				{
					//Console.WriteLine("Select " + ent.name + ent.Id);
					if (!hitList.ContainsKey(hitPoint))
						hitList.Add(hitPoint, ent.GetInstanceID());
					//break;
				}
			}
			if (hitList.Count > 0)
			{
				var entity = Root.root.First((ent) => ent.GetInstanceID() == hitList.Values[0]);

				Console.WriteLine("Select " + entity.name + entity.GetInstanceID());
				Selection.Asset = entity;
				SceneStructureView.tree.SelectedNode = SceneStructureView.flattenedTree[entity.GetInstanceID()];
			}
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