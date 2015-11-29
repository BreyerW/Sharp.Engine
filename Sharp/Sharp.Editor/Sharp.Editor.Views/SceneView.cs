using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Sharp.AssetPipeline;
using System;
using System.Runtime.InteropServices;
using Gwen.Control;
using System.Diagnostics;
using OpenTK.Input;
using Sharp.Editor;
//using PhysX;
using Sharp.Physic;

namespace Sharp.Editor.Views
{
	public class SceneView:View
	{
		
		private HashSet<Camera> camera;
		private int cell_size = 32;
		private int grid_size = 4096;

		public static HashSet<Entity> entities=new HashSet<Entity>();
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static Action OnRenderFrame;
		public static Action OnSetupMatrices;

		public static bool mouseLocked=false;
		/*DebugProc DebugCallbackInstance = DebugCallback;

		static void DebugCallback(DebugSource source, DebugType type, int id,
			DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string msg = Marshal.PtrToStringAnsi(message);
			Console.WriteLine("[GL] {0}; {1}; {2}; {3}; {4};",
				source, type, id, severity, msg);
		}*/
		public override void Initialize(){
			//ErrorOutput errorOutput = new ErrorOutput();

			//Foundation foundation = new Foundation(errorOutput);
			//physEngine= new Physics(foundation, checkRuntimeFiles: true);
			//var sceneDesc = new SceneDesc (){Gravity = new System.Numerics.Vector3(0, -9.81f, 0) };
			//physScene = physEngine.CreateScene ();

			var e = new Entity ();
			e.rotation = new Vector3 ((float)Math.PI, 0f, 0f);
			var cam=e.AddComponent<Camera> ();
			cam.SetModelviewMatrix ();
			cam.SetProjectionMatrix ();
			Camera.main = cam;
			if(OnSetupMatrices!=null)
			OnSetupMatrices ();
			base.Initialize ();
		}
		public override void OnContextCreated(int width, int height){
			//GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			//if(OnRenderFrame!=null)
			//	renderer.mesh.MVPMatrix = renderer.mesh.ModelMatrix *Camera.main.GetViewMatrix()* Camera.main.projectionMatrix;

			Camera.main.AspectRatio = (float)(canvas.Width / (float)canvas.Height);
			Camera.main.SetProjectionMatrix ();
			Camera.main.frustum = new Frustum (Camera.main.modelViewMatrix*Camera.main.projectionMatrix);
			//GL.Viewport(0, 0, width,height);

			//GL.MatrixMode(MatrixMode.Projection);
			//GL.LoadMatrix(ref Camera.main.projectionMatrix);
			if(OnSetupMatrices!=null)
			OnSetupMatrices ();
		}
		public override void Render(){
			base.Render();
			GL.ClearColor (new OpenTK.Graphics.Color4(37,37,37,255));
			GL.Clear(ClearBufferMask.DepthBufferBit|ClearBufferMask.ColorBufferBit);

			GL.CullFace (CullFaceMode.Back);
			//GL.Disable (EnableCap.Blend);
			GL.Enable (EnableCap.DepthTest);
			GL.DepthFunc (DepthFunction.Less);
			//GL.Enable(EnableCap.DebugOutput);
			//GL.Enable(EnableCap.DebugOutputSynchronous);
			Camera.main.Update();
			//GL.MatrixMode(MatrixMode.Projection);
			var projMat=Camera.main.modelViewMatrix*Camera.main.projectionMatrix;
			GL.PushMatrix ();
			GL.LoadMatrix(ref projMat);
			DrawHelper.DrawGrid(System.Drawing.Color.GhostWhite,Camera.main.entityObject.position, cell_size, grid_size);
			GL.PopMatrix ();
			if (OnRenderFrame != null) {
				OnRenderFrame ();
			}

			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			GL.DisableVertexAttribArray(2);
			GL.DisableVertexAttribArray(3);

			//GL.DebugMessageCallback(DebugCallbackInstance, IntPtr.Zero);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			//canvas.RenderCanvas ();
		}
		public override void OnResize(int width, int height){
			
			if (OnSetupMatrices != null) {
				OnSetupMatrices ();
			}
			base.OnResize(width,height);
			if (Camera.main != null && height != 0) {
				Camera.main.AspectRatio = (float)(canvas.Width / (float)canvas.Height);
				Camera.main.SetProjectionMatrix ();
			}
		}
		public override void OnKeyPressEvent (ref KeyboardState evnt)
		{

			if (evnt[OpenTK.Input.Key.Q]) 
				Camera.main.Move (0f, 1f, 0f,0.1f);
			if (evnt[OpenTK.Input.Key.E])
				Camera.main.Move(0f, -1f, 0f,0.1f);
			if (evnt[OpenTK.Input.Key.A])
				Camera.main.Move(-1f, 0f, 0f,0.1f);
			if (evnt[OpenTK.Input.Key.D])
				Camera.main.Move(1f, 0f, 0f,0.1f);
			if (evnt[OpenTK.Input.Key.W])
				Camera.main.Move(0f, 0f, -1f,0.1f);
			if (evnt[OpenTK.Input.Key.S])
				Camera.main.Move(0f, 0f, 1f,0.1f);
			if(SceneView.OnSetupMatrices!=null)
				SceneView.OnSetupMatrices ();
			if (Camera.main.moved) {
				canvas.NeedsRedraw = true;
				Camera.main.moved = false;
			}
		}
		public override void OnMouseMove (MouseMoveEventArgs evnt)
		{
			//scene.SetupMatrices ();
			if (mouseLocked) {
				Camera.main.Rotate ((float)evnt.XDelta, (float)evnt.YDelta, 0.3f);//maybe divide delta by fov?
				MainWindow.focusedView=this;
				canvas.NeedsRedraw = true;
			}
			//QueueDraw ();
			Console.WriteLine ("moveScene");
		}
		public override void OnMouseDown(MouseButtonEventArgs evnt){
			if (canvas.IsHovered && evnt.Button == MouseButton.Right) {
				mouseLocked = true;
				//canvas.NeedsRedraw = true;
			}
		//	Console.WriteLine ("down");
		}
		public override void OnMouseUp(MouseButtonEventArgs args){
				
			 if ( AssetsView.isDragging ) {
				//makeContextCurrent ();
				foreach (var asset in AssetsView.tree.SelectedChildren) {
					var eObject = new Entity ();
					eObject.position = Camera.main.ScreenToWorld (args.X,/*Allocation.Height-*/args.Y, canvas.Width, canvas.Height);
					if (asset.Content is Mesh<ushort>) {
						var renderer = eObject.AddComponent (new MeshRenderer<ushort,BasicVertexFormat> ((Mesh<ushort>)asset.Content)) as MeshRenderer<ushort,BasicVertexFormat>;
						renderer.SetModelMatrix (); //FromMatrix (scene.RootNode.Transform);
						renderer.material.shaderId = Shader.shaders ["BasicShader"].Program;
						var tex =Texture.textures["duckCM"];
						renderer.material.SetTexture ("MyTexture",tex);
						eObject.Instatiate ();
					}
				}
				AssetsView.isDragging = false;
			}
			else if(args.Button== MouseButton.Left)
				foreach (var ent in entities){

					var render=ent.GetComponent<MeshRenderer<ushort,BasicVertexFormat>> ();

					if (render != null)
					if (Camera.main.frustum.Intersect (render.mesh.bounds, render.mesh.ModelMatrix) != 0) {
						var minScreen =Camera.main.WorldToScreen (Vector3.TransformPosition(render.mesh.bounds.Min,render.mesh.ModelMatrix), canvas.Width, canvas.Height);
						var maxScreen =Camera.main.WorldToScreen (Vector3.TransformPosition(render.mesh.bounds.Max,render.mesh.ModelMatrix), canvas.Width, canvas.Height);

						var hAlign=canvas.Width / 2;
						var vAlign=canvas.Height/2;
						var localPoint=canvas.CanvasPosToLocal(args.Position);

						if ((minScreen.X+hAlign < localPoint.X && maxScreen.X+hAlign > localPoint.X) || (minScreen.X+hAlign > localPoint.X && maxScreen.X+hAlign < localPoint.X) )
						if ((vAlign- minScreen.Y > localPoint.Y && vAlign- maxScreen.Y < localPoint.Y) || (vAlign- minScreen.Y < localPoint.Y && vAlign- maxScreen.Y > localPoint.Y)) {
							Console.WriteLine ("bum");
							Selection.assets.Clear();
							Selection.assets.Add (ent);
							canvas.NeedsRedraw = true;
							mouseLocked = false;
							break;
						}
					}
				}
			mouseLocked = false;
			MainWindow.focusedView = null;
		}
	}
}

