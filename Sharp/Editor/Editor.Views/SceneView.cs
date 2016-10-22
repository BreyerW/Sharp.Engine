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
		public static Action OnAddedEntity;
		public static Action OnRemovedEntity;
		//public static Scene physScene;
		//public static Physics physEngine;

		public static Action OnUpdate;
		public static Action OnRenderFrame;
		public static Action OnSetupMatrices;

		public static bool mouseLocked=false;
		private static System.Drawing.Point? locPos=null;
		private static int selectedAxisId = 0;
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

			var eLight = new Entity ();
			eLight.name="Directional Light";
			eLight.Position = cam.entityObject.Position;
			var light = eLight.AddComponent<Light> ();
			eLight.Instatiate ();
			if(OnSetupMatrices!=null)
			OnSetupMatrices ();
			base.Initialize ();
            
        }
		public override void OnContextCreated(int width, int height){
			//GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			//if(OnRenderFrame!=null)
			//	renderer.mesh.MVPMatrix = renderer.mesh.ModelMatrix *Camera.main.GetViewMatrix()* Camera.main.projectionMatrix;

			Camera.main.AspectRatio = (float)(panel.Width / (float)panel.Height);
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

			Camera.main.Update();

			if (locPos.HasValue) {
			backendRenderer.ClearBuffer ();
			backendRenderer.SetFlatColorState ();
			backendRenderer.ChangeShader ();
				if (SceneStructureView.tree.SelectedChildren.Any()) {
					
					System.Drawing.Color xColor=System.Drawing.Color.Red, yColor=System.Drawing.Color.LimeGreen, zColor=System.Drawing.Color.Blue;
					for (int id = 1; id < 4; id++) {
					var color = System.Drawing.Color.FromArgb ((id & 0x000000FF)>>00,(id & 0x0000FF00) >> 08,(id & 0x00FF0000) >> 16);
						
						switch (id) {
							case 1:
								xColor=color;
								break;
							case 2:
								yColor = color;
								break;

							case 3:
								zColor = color;
								break;
							}
						}

					foreach (var selected in SceneStructureView.tree.SelectedChildren) {
						var entity = selected.Content as Entity;
						var mvpMat = entity.ModelMatrix * Camera.main.modelViewMatrix * Camera.main.projectionMatrix;

						MainEditorView.editorBackendRenderer.LoadMatrix (ref mvpMat);

						float cameraObjectDistance =(Camera.main.entityObject.Position-entity.Position).Length;
						DrawHelper.DrawTranslationGizmo (3, cameraObjectDistance,xColor ,yColor,zColor);

						MainEditorView.editorBackendRenderer.UnloadMatrix();
					}
					backendRenderer.FinishCommands ();
				if (locPos.HasValue) {
					var pixel = backendRenderer.ReadPixels (locPos.Value.X,locPos.Value.Y, 1, 1);
					int index = (((int)pixel[0])<<00) + (((int)pixel[1]) << 08) + ((((int)pixel[2]) << 16));
						if (index>0 && index < 4) {
							selectedAxisId = index;
							mouseLocked = true;
						}
						else
							selectedAxisId = 0;
				}
				}
				if(locPos.HasValue && selectedAxisId<1) {
					var orig =Camera.main.entityObject.Position;
					var end = Camera.main.ScreenToWorld (locPos.Value.X,locPos.Value.Y, panel.Width, panel.Height);
					var ray = new Ray (orig, (end-orig).Normalized());
					foreach (var ent in entities) {
						dynamic render = ent.GetComponent(typeof(MeshRenderer<,>));
						if (render != null && render.mesh.bounds.Intersect(ref ray, ref ent.ModelMatrix)) {
							var id = entities.IndexOf (ent);
							Selection.Asset=entities[id];
							//Selection.assets.Add (ent);
							SceneStructureView.tree.UnselectAll ();
							SceneStructureView.tree.FindNodeByContent (ent).IsSelected=true;
							break;
						}
					}
				}
				locPos = null;
			}
			backendRenderer.ClearBuffer ();
			backendRenderer.SetStandardState ();
			var projMat = Camera.main.modelViewMatrix * Camera.main.projectionMatrix;

			DrawHelper.DrawGrid (System.Drawing.Color.GhostWhite, Camera.main.entityObject.Position, cell_size, grid_size, ref projMat);

            OnRenderFrame?.Invoke ();


			if (SceneStructureView.tree.SelectedChildren.Any()) {

				System.Drawing.Color xColor=System.Drawing.Color.Red, yColor=System.Drawing.Color.LimeGreen, zColor=System.Drawing.Color.Blue;

				foreach (var selected in SceneStructureView.tree.SelectedChildren) {
					var entity = selected.Content as Entity;
					var mvpMat = entity.ModelMatrix * Camera.main.modelViewMatrix * Camera.main.projectionMatrix;
					MainEditorView.editorBackendRenderer.LoadMatrix (ref mvpMat);
					//var tmpMat =mesh.ModelMatrix* Camera.main.modelViewMatrix * Camera.main.projectionMatrix;
					GL.Enable(EnableCap.Blend);
					GL.Disable(EnableCap.DepthTest);
					GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

					float cameraObjectDistance =(Camera.main.entityObject.Position-entity.Position).Length;

					DrawHelper.DrawTranslationGizmo (3,cameraObjectDistance/11,xColor ,yColor,zColor);
					GL.Enable(EnableCap.DepthTest);
					//dynamic renderer = entity.GetComponent (typeof(MeshRenderer<,>));

					//if (renderer == null)
					//	return;
					//DrawHelper.DrawBox (renderer.mesh.bounds.Min, renderer.mesh.bounds.Max);

					MainEditorView.editorBackendRenderer.UnloadMatrix ();
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
				Camera.main.AspectRatio = (float)(panel.Width / (float)panel.Height);
				Camera.main.SetProjectionMatrix ();
			}
		}
		public override void OnKeyPressEvent (ref KeyboardState evnt)
		{
			if (Camera.main.moved) {
				MainEditorView.canvas.NeedsRedraw = true;
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
		int oldX;
		int oldY;
		public override void OnMouseMove (MouseMoveEventArgs evnt)
		{
			//Console.WriteLine ("locked? "+mouseLocked);
			if (mouseLocked) {
				if (selectedAxisId==0) {
					Camera.main.Rotate ((float)evnt.XDelta, (float)evnt.YDelta, 0.3f);//maybe divide delta by fov?
					SceneView.OnSetupMatrices?.Invoke ();

				}else{
					
					var constTValue = 1f;
					var constRValue = 1f;
					var constSValue = 1f;

					var deltaX = oldX - evnt.X;
					var deltaY = oldY - evnt.Y;
					if(evnt.XDelta!=0 && evnt.YDelta!=0)
					foreach (var selected in SceneStructureView.tree.SelectedChildren)
					{
						var entity = selected.Content as Entity;
							switch (selectedAxisId) {
							case 1:
								entity.Position += new Vector3 (-deltaX * constTValue,0,0);
								break;
							case 2:
								entity.Position += new Vector3 (0, deltaY * constTValue, 0);
								break;
							case 3:
								entity.Position += new Vector3 (0, 0, deltaX* constTValue);
								break;
							}
					}
				}
				MainWindow.focusedView=this;
				MainEditorView.canvas.NeedsRedraw = true;
			}
			oldX = evnt.X;
			oldY = evnt.Y;
		}
		public override void OnMouseDown(MouseButtonEventArgs evnt){
			if (evnt.Button == MouseButton.Right) {//canvas.IsHovered
				mouseLocked = true;
				selectedAxisId = 0;
			} else if (evnt.Button == MouseButton.Left) {
				mouseLocked = false;
				locPos =panel.CanvasPosToLocal(evnt.Position);
                locPos = new System.Drawing.Point(locPos.Value.X,locPos.Value.Y-29);
                MainEditorView.canvas.NeedsRedraw = true;
			}
		//	Console.WriteLine ("down");
		}
		public override void OnMouseUp(MouseButtonEventArgs args){
            Console.WriteLine("up? " + AssetsView.isDragging);
            if (AssetsView.isDragging) {
				//makeContextCurrent ();
				foreach (var asset in AssetsView.tree.SelectedChildren) {
					var eObject = new Entity ();
					var locPos =MainEditorView.canvas.CanvasPosToLocal (args.Position);
					Camera.main.SetModelviewMatrix ();
					var orig =Camera.main.entityObject.Position;
					var dir = (Camera.main.ScreenToWorld (locPos.X, locPos.Y, panel.Width, panel.Height)-orig).Normalized ();
					eObject.Position =orig+dir*Camera.main.ZFar*0.1f;
					if (asset.Content.GetType () == typeof(Skeleton)) {
						var skele=(Skeleton)asset.Content;
						var renderer = eObject.AddComponent( new SkeletonRenderer (ref skele));
						var shader=Shader.shaders ["SkeletonShader"];
						SceneView.backendRenderer.Allocate (ref shader);
					}
					else if (asset.Content.GetType().GetGenericTypeDefinition() == typeof(Mesh<>)) {
						var mesh=asset.Content as IAsset;
						var renderer = eObject.AddComponent (new MeshRenderer<ushort,BasicVertexFormat> (mesh)) as MeshRenderer<ushort,BasicVertexFormat>;
						 //FromMatrix (scene.RootNode.Transform);
						var shader=Shader.shaders ["BasicShader"];
						SceneView.backendRenderer.Allocate (ref shader);
						renderer.material = new Material (shader.Program);
						var tex = Texture.textures ["duckCM"];
						renderer.material.SetProperty ("MyTexture",ref tex);
					}
					eObject.Instatiate ();
				}
				AssetsView.isDragging = false;
			}
			mouseLocked = false;
			MainWindow.focusedView = null;
		}
	}
}

