using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpAsset;
using System;
using System.Runtime.InteropServices;
using Gwen.Control;
using System.Diagnostics;
using OpenTK.Input;
using Sharp.Editor;
//using PhysX;
using Sharp.Physic;
using System.Linq;
using SharpSL.BackendRenderers;

namespace Sharp.Editor.Views
{
	public class SceneView:View
	{
		
		private HashSet<Camera> camera;
		private int cell_size = 32;
		private int grid_size = 4096;

		public static List<Entity> entities=new List<Entity>();
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static Action OnRenderFrame;
		public static Action OnSetupMatrices;

		public static bool mouseLocked=false;
		private Vector3 normalizedMoveDir=Vector3.Zero;

		public static IBackendRenderer backendRenderer;
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
			e.Rotation = new Vector3 ((float)Math.PI, 0f, 0f);
			var cam=e.AddComponent<Camera> ();
			cam.SetModelviewMatrix ();
			cam.SetProjectionMatrix ();
			e.name="Main Camera";
			e.OnTransformChanged +=((sender, evnt) =>cam.SetModelviewMatrix());
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
			GL.LoadMatrix(ref projMat);
			GL.PushMatrix ();
			DrawHelper.DrawGrid(System.Drawing.Color.GhostWhite,Camera.main.entityObject.Position, cell_size, grid_size);
			GL.PopMatrix ();

			OnRenderFrame?.Invoke ();
				
			if (SceneStructureView.tree.SelectedChildren.Any()) {
				foreach (var selected in SceneStructureView.tree.SelectedChildren) {
					var entity = selected.Content() as Entity;

					GL.LoadMatrix (ref entity.MVPMatrix);
					GL.PushMatrix ();
					//var tmpMat =mesh.ModelMatrix* Camera.main.modelViewMatrix * Camera.main.projectionMatrix;
					dynamic renderer = entity.GetComponent (typeof(MeshRenderer<,>));
					if (renderer == null)
						return;
					DrawHelper.DrawBox (renderer.mesh.bounds.Min, renderer.mesh.bounds.Max);
					//float cameraObjectDistance =(Camera.main.entityObject.position-entityObject.position).Length;
					//float worldSize = (float)(2 * Math.Tan((double)(Camera.main.FieldOfView / 2.0)) * cameraObjectDistance);
					//Manipulators.gizmoScale =0.25f* worldSize;
					DrawHelper.DrawSphere (30, 25, 25, System.Drawing.Color.Aqua);
					GL.PopMatrix ();
				}
			}

		/*	GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
			GL.DisableVertexAttribArray(2);
			GL.DisableVertexAttribArray(3);

			//GL.DebugMessageCallback(DebugCallbackInstance, IntPtr.Zero);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);*/

			//canvas.RenderCanvas ();
		}
		public override void OnResize(int width, int height){
			
			OnSetupMatrices?.Invoke ();
			base.OnResize(width,height);
			if (Camera.main != null && height != 0) {
				Camera.main.AspectRatio = (float)(canvas.Width / (float)canvas.Height);
				Camera.main.SetProjectionMatrix ();
			}
		}
		public override void OnKeyPressEvent (ref KeyboardState evnt)
		{
			if (Camera.main.moved) {
				canvas.NeedsRedraw = true;
				Camera.main.moved = false;
				Camera.main.SetModelviewMatrix ();

			}
			//if (!canvas.IsHovered)
				//return;
			if (evnt[OpenTK.Input.Key.Q]) 
				Camera.main.Move (0f, 1f, 0f,0.03f);
			if (evnt[OpenTK.Input.Key.E])
				Camera.main.Move(0f, -1f, 0f,0.03f);
			if (evnt[OpenTK.Input.Key.A])
				Camera.main.Move(-1f, 0f, 0f,0.03f);
			if (evnt[OpenTK.Input.Key.D])
				Camera.main.Move(1f, 0f, 0f,0.03f);
			if (evnt[OpenTK.Input.Key.W])
				Camera.main.Move(0f, 0f, -1f,0.03f);
			if (evnt[OpenTK.Input.Key.S])
				Camera.main.Move(0f, 0f, 1f,0.03f);
			SceneView.OnSetupMatrices?.Invoke ();
		}
		public static double AngleBetween(Vector3 vector1, Vector3 vector2)
		{
			var angle= Math.Atan2(vector1.Y, vector1.X) - Math.Atan2(vector2.Y, vector2.X) * (180 / Math.PI);
			if (angle < 0){
				angle = angle + 360;
			}
			return angle;
		}
		public override void OnMouseMove (MouseMoveEventArgs evnt)
		{
			//scene.SetupMatrices ();
			if (mouseLocked) {
				if (normalizedMoveDir == Vector3.Zero) {
					Camera.main.Rotate ((float)evnt.XDelta, (float)evnt.YDelta, 0.3f);//maybe divide delta by fov?
					SceneView.OnSetupMatrices?.Invoke ();
				}else{
					var deltaDir = new Vector2 ((float)evnt.XDelta, (float)evnt.YDelta).Normalized();
					var screenDir = Camera.main.WorldToScreen (normalizedMoveDir,canvas.Width,canvas.Height);
					var angle=Vector2.Dot(deltaDir,screenDir.Xy.Normalized());
					if(evnt.XDelta!=0 && evnt.YDelta!=0)
					foreach (var selected in SceneStructureView.tree.SelectedChildren)
					{
						var entity = selected.Content() as Entity;

						if((angle>=-1 && angle<=0))
							entity.Position +=normalizedMoveDir*deltaDir.LengthFast;
						else
							entity.Position -=normalizedMoveDir*deltaDir.LengthFast;
					}
				}
				MainWindow.focusedView=this;
				canvas.NeedsRedraw = true;
			}
		}
		public override void OnMouseDown(MouseButtonEventArgs evnt){
			if (evnt.Button == MouseButton.Right) {//canvas.IsHovered
				mouseLocked = true;
			} else if (evnt.Button == MouseButton.Left) {
				mouseLocked = true;
				var locPos = canvas.CanvasPosToLocal (evnt.Position);
				var orig = Camera.main.entityObject.Position;
				var end = Camera.main.ScreenToWorld (locPos.X, locPos.Y, canvas.Width, canvas.Height);
				var ray = new Ray (orig, (end - orig).Normalized ());
				var intersectPoint = Vector3.Zero;
				var zero = Vector3.Zero;
				if (SceneStructureView.tree.SelectedChildren.Any ())
					foreach (var selected in SceneStructureView.tree.SelectedChildren) {
						var entity = selected.Content() as Entity;
						dynamic render = entity.GetComponent (typeof(MeshRenderer<,>));

						if (render != null && BoundingBox.Intersect (ref ray, ref zero, 30, ref entity.ModelMatrix, out intersectPoint)) {
							normalizedMoveDir = (intersectPoint - Vector3.TransformPosition(zero,entity.ModelMatrix)).Normalized();
							break;
						}
					}
			}
		//	Console.WriteLine ("down");
		}
		public override void OnMouseUp(MouseButtonEventArgs args){
			if (AssetsView.isDragging) {
				//makeContextCurrent ();
				foreach (var asset in AssetsView.tree.SelectedChildren) {
					var eObject = new Entity ();
					var locPos = canvas.CanvasPosToLocal (args.Position);
					Camera.main.SetModelviewMatrix ();
					var orig =Camera.main.entityObject.Position;
					var dir = (Camera.main.ScreenToWorld (locPos.X, locPos.Y, canvas.Width, canvas.Height)-orig).Normalized ();
					eObject.Position =orig+dir*Camera.main.ZFar*0.1f;
					if (asset.Content ().GetType () == typeof(Skeleton)) {
						var skele=(Skeleton)asset.Content();
						var renderer = eObject.AddComponent( new SkeletonRenderer (ref skele));
						var shader=Shader.shaders ["SkeletonShader"];
						SceneView.backendRenderer.Allocate (ref shader);
					}
					else if (asset.Content().GetType().GetGenericTypeDefinition() == typeof(Mesh<>)) {
						var mesh=asset.Content() as IAsset;
						var renderer = eObject.AddComponent (new MeshRenderer<ushort,BasicVertexFormat> (mesh)) as MeshRenderer<ushort,BasicVertexFormat>;
						 //FromMatrix (scene.RootNode.Transform);
						var shader=Shader.shaders ["BasicShader"];
						SceneView.backendRenderer.Allocate (ref shader);
						renderer.material = new Material (shader.Program);
						var tex = Texture.textures ["duckCM"];
						renderer.material.SetShaderProperty ("MyTexture",ref tex);
					}
					eObject.Instatiate ();
				}
				AssetsView.isDragging = false;
			} else if (args.Button == MouseButton.Left) {
				var locPos = canvas.CanvasPosToLocal (args.Position);
				var orig =Camera.main.entityObject.Position;
				var end = Camera.main.ScreenToWorld (locPos.X,locPos.Y, canvas.Width, canvas.Height);
				var ray = new Ray (orig, (end-orig).Normalized());
				foreach (var ent in entities) {
					dynamic render = ent.GetComponent(typeof(MeshRenderer<,>));
						if (render != null && render.mesh.bounds.Intersect(ref ray, ref ent.ModelMatrix)) {
						var id = entities.IndexOf (ent);
						Selection.Asset=()=>entities[id];
							//Selection.assets.Add (ent);
						SceneStructureView.tree.UnselectAll ();
						SceneStructureView.tree.FindNodeByContent (ent).IsSelected=true;
							canvas.NeedsRedraw = true;
							mouseLocked = false;
							break;
						}
				}
			}
			mouseLocked = false;
			MainWindow.focusedView = null;
			normalizedMoveDir = Vector3.Zero;
		}
	}
}

