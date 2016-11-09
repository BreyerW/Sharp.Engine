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
    public class SceneView : View
    {
        internal static IBackendRenderer backendRenderer;

        private HashSet<Camera> camera;
        private int cell_size = 32;
        private int grid_size = 4096;


        public static HashSet<Entity> entities = new HashSet<Entity>();
        public static Action OnAddedEntity;
        public static Action OnRemovedEntity;
        //public static Scene physScene;
        //public static Physics physEngine;

        public static Action OnUpdate;
        public static Action OnRenderFrame;
        public static Action OnSetupMatrices;

        public static bool mouseLocked = false;
        public static bool globalMode = false;

        private static System.Drawing.Point? locPos = null;
        private Vector3 normalizedMoveDir = Vector3.Zero;


        /*DebugProc DebugCallbackInstance = DebugCallback;

		static void DebugCallback(DebugSource source, DebugType type, int id,
			DebugSeverity severity, int length, IntPtr message, IntPtr userParam)
		{
			string msg = Marshal.PtrToStringAnsi(message);
			Console.WriteLine("[GL] {0}; {1}; {2}; {3}; {4};",
				source, type, id, severity, msg);
		}*/
        public override void Initialize()
        {
            //ErrorOutput errorOutput = new ErrorOutput();

            //Foundation foundation = new Foundation(errorOutput);
            //physEngine= new Physics(foundation, checkRuntimeFiles: true);
            //var sceneDesc = new SceneDesc (){Gravity = new System.Numerics.Vector3(0, -9.81f, 0) };
            //physScene = physEngine.CreateScene ();

            var e = new Entity();
            e.Rotation = new Vector3((float)Math.PI, 0f, 0f);
            var cam = e.AddComponent<Camera>();
            cam.SetModelviewMatrix();
            cam.SetProjectionMatrix();
            e.name = "Main Camera";
            e.OnTransformChanged += ((sender, evnt) => cam.SetModelviewMatrix());
            Camera.main = cam;

            var eLight = new Entity();
            eLight.name = "Directional Light";
            eLight.Position = cam.entityObject.Position;
            var light = eLight.AddComponent<Light>();
            eLight.Instatiate();


            OnSetupMatrices?.Invoke();
            base.Initialize();

        }
        public override void OnContextCreated(int width, int height)
        {
            Camera.main.AspectRatio = (float)(panel.Width / (float)panel.Height);
            Camera.main.SetProjectionMatrix();
            Camera.main.frustum = new Frustum(Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix);
            OnSetupMatrices?.Invoke();
        }
        public override void Render()
        {
            base.Render();

            Camera.main.Update();

            if (locPos.HasValue)
            {
                if (!PickTestForGizmo())
                    PickTestForObject();
                locPos = null;
            }
            backendRenderer.ClearBuffer();
            backendRenderer.SetStandardState();
            var projMat = Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;

            DrawHelper.DrawGrid(System.Drawing.Color.GhostWhite, Camera.main.entityObject.Position, cell_size, grid_size, ref projMat);

            OnRenderFrame?.Invoke();

            if (SceneStructureView.tree.SelectedChildren.Any())
            {
                foreach (var selected in SceneStructureView.tree.SelectedChildren)
                {
                    var entity = selected.Content as Entity;
                    var mvpMat = (globalMode ? entity.ModelMatrix.ClearRotation() : entity.ModelMatrix).ClearScale() * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
                    MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);
                    GL.Enable(EnableCap.Blend);
                    GL.Clear(ClearBufferMask.DepthBufferBit);

                    Manipulators.DrawCombinedGizmos(entity.Position);
                    MainEditorView.editorBackendRenderer.UnloadMatrix();

                    /*dynamic renderer = entity.GetComponent(typeof(MeshRenderer<,>));
                    if (renderer != null)
                    {
                        var max = renderer.mesh.bounds.Max;
                        var min = renderer.mesh.bounds.Min;
                        DrawHelper.DrawBox(min, max);
                    }*/
                }
            }

            //GL.DebugMessageCallback(DebugCallbackInstance, IntPtr.Zero);
        }
        private bool PickTestForGizmo()
        {
            backendRenderer.ClearBuffer();
            backendRenderer.SetFlatColorState();
            backendRenderer.ChangeShader();
            if (SceneStructureView.tree.SelectedChildren.Any())
            {
                System.Drawing.Color xColor = System.Drawing.Color.Red, yColor = System.Drawing.Color.LimeGreen, zColor = System.Drawing.Color.Blue;
                System.Drawing.Color xRotColor = System.Drawing.Color.Red, yRotColor = System.Drawing.Color.LimeGreen, zRotColor = System.Drawing.Color.Blue;
                System.Drawing.Color xScaleColor = System.Drawing.Color.Red, yScaleColor = System.Drawing.Color.LimeGreen, zScaleColor = System.Drawing.Color.Blue;
                System.Drawing.Color color;
                for (int id = 1; id < 10; id++)
                {
                    color = System.Drawing.Color.FromArgb((id & 0x000000FF) >> 00, (id & 0x0000FF00) >> 08, (id & 0x00FF0000) >> 16);

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

                foreach (var selected in SceneStructureView.tree.SelectedChildren)
                {
                    var entity = selected.Content as Entity;
                    var mvpMat = (globalMode ? entity.ModelMatrix.ClearRotation() : entity.ModelMatrix).ClearScale() * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;

                    MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);

                    Manipulators.DrawCombinedGizmos(entity.Position, xColor, yColor, zColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);

                    MainEditorView.editorBackendRenderer.UnloadMatrix();
                }
                backendRenderer.FinishCommands();
                // if (locPos.HasValue)
                // {
                var pixel = backendRenderer.ReadPixels(locPos.Value.X, locPos.Value.Y, 1, 1);
                int index = (((int)pixel[0]) << 00) + (((int)pixel[1]) << 08) + ((((int)pixel[2]) << 16));
                if (index > 0 && index < 10)
                {
                    Manipulators.selectedAxisId = index;
                    return mouseLocked = true;
                }
                else
                    Manipulators.selectedAxisId = 0;
                //}
            }
            return false;
        }
        private void PickTestForObject()
        {
            // if (locPos.HasValue && selectedAxisId < 1)
            // {
            var orig = Camera.main.entityObject.Position;
            var end = Camera.main.ScreenToWorld(locPos.Value.X, locPos.Value.Y, panel.Width, panel.Height);
            var ray = new Ray(orig, (end - orig).Normalized());
            var hitList = new SortedList<Vector3, int>(new OrderByDistanceToCamera());
            foreach (var ent in entities)
            {
                dynamic render = ent.GetComponent(typeof(MeshRenderer<,>));
                Vector3 hitPoint = Vector3.Zero;
                if (render != null && render.mesh.bounds.Intersect(ref ray, ref ent.ModelMatrix, out hitPoint))
                {
                    Console.WriteLine("Select " + ent.name + ent.id);
                    hitList.Add(hitPoint, ent.id);
                    //break;
                }
            }
            if (hitList.Count > 0)
            {

                //var id = entities.(ent);
                var entity = entities.First((ent) => ent.id == hitList.Values[0]);
                Console.WriteLine("Select " + entity.name + entity.id);
                Selection.Asset = entity;
                //Selection.assets.Add (ent);
                SceneStructureView.tree.UnselectAll();
                SceneStructureView.tree.FindNodeByContent(entity).IsSelected = true;
            }
            //}
        }
        public override void OnResize(int width, int height)
        {
            OnSetupMatrices?.Invoke();
            base.OnResize(width, height);
            if (Camera.main != null && height != 0)
            {
                Camera.main.AspectRatio = (float)(panel.Width / (float)panel.Height);
                Camera.main.SetProjectionMatrix();
            }
        }
        public override void OnKeyPressEvent(ref KeyboardState evnt)
        {
            if (Camera.main.moved)
            {
                MainEditorView.canvas.NeedsRedraw = true;
                Camera.main.moved = false;
                Camera.main.SetModelviewMatrix();

            }
            //if (!canvas.IsHovered)
            //return;
            if (evnt[OpenTK.Input.Key.Q])
                Camera.main.Move(0f, 1f, 0f, 0.01f);
            if (evnt[OpenTK.Input.Key.E])
                Camera.main.Move(0f, -1f, 0f, 0.01f);
            if (evnt[OpenTK.Input.Key.A])
                Camera.main.Move(-1f, 0f, 0f, 0.01f);
            if (evnt[OpenTK.Input.Key.D])
                Camera.main.Move(1f, 0f, 0f, 0.01f);
            if (evnt[OpenTK.Input.Key.W])
                Camera.main.Move(0f, 0f, -1f, 0.01f);
            if (evnt[OpenTK.Input.Key.S])
                Camera.main.Move(0f, 0f, 1f, 0.01f);
            OnSetupMatrices?.Invoke();
        }

        int oldX;
        int oldY;
        public override void OnMouseMove(MouseMoveEventArgs evnt)
        {
            //Console.WriteLine ("locked? "+mouseLocked);
            if (mouseLocked)
            {
                if (Manipulators.selectedAxisId == 0)
                {
                    Camera.main.Rotate((float)evnt.XDelta, (float)evnt.YDelta, 0.3f);//maybe divide delta by fov?
                    SceneView.OnSetupMatrices?.Invoke();
                }
                else//simple, precise, snapping
                {
                    if (evnt.XDelta != 0 || evnt.YDelta != 0)
                    {
                        var orig = Camera.main.entityObject.Position;
                        var localMouse = panel.CanvasPosToLocal(evnt.Position);
                        localMouse = new System.Drawing.Point(localMouse.X, localMouse.Y - 29);
                        var start = Camera.main.ScreenToWorld(localMouse.X, localMouse.Y, panel.Width, panel.Height, 1);
                        var ray = new Ray(orig, (start - orig).Normalized());

                        foreach (var selected in SceneStructureView.tree.SelectedChildren)
                        {
                            var entity = selected.Content as Entity;

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
                MainWindow.focusedView = this;
                MainEditorView.canvas.NeedsRedraw = true;
            }
            oldX = evnt.X;
            oldY = evnt.Y;
        }

        public override void OnMouseDown(MouseButtonEventArgs evnt)
        {
            if (evnt.Button == MouseButton.Right)
            {//canvas.IsHovered
                mouseLocked = true;
                Manipulators.selectedAxisId = 0;
            }
            else if (evnt.Button == MouseButton.Left)
            {
                mouseLocked = false;
                locPos = panel.CanvasPosToLocal(evnt.Position);
                locPos = new System.Drawing.Point(locPos.Value.X, locPos.Value.Y - 29);
                MainEditorView.canvas.NeedsRedraw = true;
            }
            //	Console.WriteLine ("down");
        }
        public override void OnMouseUp(MouseButtonEventArgs args)
        { //bug czasem blokuje sie myszka na znaczonym obiekcie i nie da sie zaznaczyc innego ma to cos wspolnego z manipulatorem
            Console.WriteLine("up? " + AssetsView.isDragging);
            if (AssetsView.isDragging)
            {
                //makeContextCurrent ();
                foreach (var asset in AssetsView.tree.SelectedChildren)
                {
                    var eObject = new Entity();
                    var locPos = panel.CanvasPosToLocal(args.Position);
                    Camera.main.SetModelviewMatrix();
                    var orig = Camera.main.entityObject.Position;
                    var dir = (Camera.main.ScreenToWorld(locPos.X, locPos.Y, panel.Width, panel.Height) - orig).Normalized();
                    eObject.Position = orig + dir * Camera.main.ZFar * 0.1f; //-3 * Vector3.UnitZ; //
                    if (asset.Content.GetType() == typeof(Skeleton))
                    {
                        var skele = (Skeleton)asset.Content;
                        var renderer = eObject.AddComponent(new SkeletonRenderer(ref skele));
                        var shader = Shader.getAsset("SkeletonShader");
                    }
                    else if (asset.Content.GetType().GetGenericTypeDefinition() == typeof(Mesh<>))
                    {

                        var mesh = asset.Content as IAsset;
                        var renderer = eObject.AddComponent(new MeshRenderer<ushort, BasicVertexFormat>(mesh)) as MeshRenderer<ushort, BasicVertexFormat>;
                        //FromMatrix (scene.RootNode.Transform);
                        var shader = Shader.getAsset("TextureOnlyShader");
                        renderer.material = new Material();
                        renderer.material.Shader = shader;
                        var tex = Texture.getAsset("duckCM");
                        renderer.material.SetProperty("MyTexture", ref tex);
                    }
                    eObject.Instatiate();
                }
                AssetsView.isDragging = false;
            }
            Manipulators.Reset();
            mouseLocked = false;
            MainWindow.focusedView = null;
        }
    }
    class OrderByDistanceToCamera : IComparer<Vector3>
    {

        public int Compare(Vector3 x, Vector3 y)
        {
            var xDistance = (x - Camera.main.entityObject.Position).Length;
            var yDistance = (y - Camera.main.entityObject.Position).Length;
            if (xDistance > yDistance) return 1;
            else if (xDistance < yDistance) return -1;
            else return 0;
        }
    }
}

