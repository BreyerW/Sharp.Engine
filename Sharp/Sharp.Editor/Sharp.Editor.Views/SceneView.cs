using System.Collections.Generic;
using OpenTK;
using System;
using OpenTK.Input;
using Squid;

//using PhysX;
using SharpAsset.Pipeline;
using System.Linq;

namespace Sharp.Editor.Views
{
    public class SceneView : View
    {
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

        public SceneView(uint attachToWindow) : base(attachToWindow)
        {
        }

        public override void Initialize()
        {
            //ErrorOutput errorOutput = new ErrorOutput();

            //Foundation foundation = new Foundation(errorOutput);
            //physEngine= new Physics(foundation, checkRuntimeFiles: true);
            //var sceneDesc = new SceneDesc (){Gravity = new System.Numerics.Vector3(0, -9.81f, 0) };
            //physScene = physEngine.CreateScene ();
            //panel.Scissor = true;

            var eLight = new Entity();
            eLight.name = "Directional Light";
            eLight.Position = Camera.main.entityObject.Position;
            var light = eLight.AddComponent<Light>();
            eLight.Instatiate();
            panel.SizeChanged += (sender) => OnResize(sender.Size.x, sender.Size.y);
            panel.MouseDown += (sender, args) => OnMouseDown(args.Button);
            OnSetupMatrices?.Invoke();
            base.Initialize();
        }

        public override void Render()
        {
            if (!panel.IsVisible) return;
            base.Render();
            MainWindow.backendRenderer.Viewport(panel.Location.x, Camera.main.height - (panel.Location.y + panel.Size.y), panel.Size.x, panel.Size.y);
            if (locPos.HasValue)
            {
                if (!PickTestForGizmo())
                    PickTestForObject();
                locPos = null;
            }
            MainWindow.backendRenderer.ClearBuffer();

            MainWindow.backendRenderer.SetStandardState();
            var projMat = Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
            DrawHelper.DrawGrid(Color.White, Camera.main.entityObject.Position, cell_size, grid_size, ref projMat);

            OnRenderFrame?.Invoke();
            /*
                        /*if (SceneStructureView.tree.SelectedChildren.Any())
                        {
                            foreach (var selected in SceneStructureView.tree.SelectedChildren)
                            {
                                var entity = selected.Content as Entity;
                                var mvpMat = (globalMode ? entity.ModelMatrix.ClearRotation() : entity.ModelMatrix).ClearScale() * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;

                                MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);
                                MainWindow.backendRenderer.ClearDepth();
                                Manipulators.DrawCombinedGizmos(entity);
                                MainEditorView.editorBackendRenderer.UnloadMatrix();

                                /*dynamic renderer = entity.GetComponent(typeof(MeshRenderer<,>));
                                if (renderer != null)
                                {
                                    var max = renderer.mesh.bounds.Max;
                                    var min = renderer.mesh.bounds.Min;
                                    DrawHelper.DrawBox(min, max);
                                }*
                            }
                        }*/

            //GL.DebugMessageCallback(DebugCallbackInstance, IntPtr.Zero);
            MainWindow.backendRenderer.Viewport(0, 0, panel.Desktop.Size.x, panel.Desktop.Size.y);
        }

        private bool PickTestForGizmo()
        {
            MainWindow.backendRenderer.ClearBuffer();
            MainWindow.backendRenderer.SetFlatColorState();
            MainWindow.backendRenderer.ChangeShader();
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
                /*if(SceneStructureView.tree.SelectedNode!=null)
                {
                    var entity = selected.Content as Entity;
                    var mvpMat = (globalMode ? entity.ModelMatrix.ClearRotation() : entity.ModelMatrix).ClearScale() * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;

                    MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);

                    Manipulators.DrawCombinedGizmos(entity, xColor, yColor, zColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);

                    MainEditorView.editorBackendRenderer.UnloadMatrix();
                }*/
                MainWindow.backendRenderer.FinishCommands();
                if (locPos.HasValue)
                {
                    var pixel = MainWindow.backendRenderer.ReadPixels(locPos.Value.x, locPos.Value.y, 1, 1);
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
            var orig = Camera.main.entityObject.Position;
            var end = Camera.main.ScreenToWorld(locPos.Value.x, locPos.Value.y, panel.Size.x, panel.Size.y);
            var ray = new Ray(orig, (end - orig).Normalized());
            var hitList = new SortedList<Vector3, int>(new OrderByDistanceToCamera());
            foreach (var ent in entities)
            {
                var render = ent.GetComponent<MeshRenderer>();
                Vector3 hitPoint = Vector3.Zero;
                if (render != null && render.mesh.bounds.Intersect(ref ray, ref ent.ModelMatrix, out hitPoint))
                {
                    Console.WriteLine("Select " + ent.name + ent.id);
                    if (!hitList.ContainsKey(hitPoint))
                        hitList.Add(hitPoint, ent.id);
                    //break;
                }
            }
            if (hitList.Count > 0)
            {
                var entity = entities.First((ent) => ent.id == hitList.Values[0]);
                Console.WriteLine("Select " + entity.name + entity.id);
                Selection.Asset = entity;
                //SceneStructureView.tree.UnselectAll();
                //SceneStructureView.tree.FindNodeByContent(entity).IsSelected = true;
            }
        }

        public override void OnResize(int width, int height)
        {
            OnSetupMatrices?.Invoke();
            if (Camera.main != null)
            {
                Camera.main.AspectRatio = (float)width / height;
                Camera.main.SetProjectionMatrix();
                Console.WriteLine(Camera.main.AspectRatio);
            }
            Camera.main.frustum = new Frustum(Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix);
        }

        public override void OnKeyPressEvent(ref byte[] keyboardState)
        {
            if (!panel.IsVisible) return;
            if (Camera.main.moved)
            {
                Camera.main.moved = false;
                Camera.main.SetModelviewMatrix();
            }
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_Q] is 1)
                Camera.main.Move(0f, 1f, 0f);
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_E] is 1)
                Camera.main.Move(0f, -1f, 0f);
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_A] is 1)
                Camera.main.Move(-1f, 0f, 0f);
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_D] is 1)
                Camera.main.Move(1f, 0f, 0f);
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_W] is 1)
                Camera.main.Move(0f, 0f, -1f);
            if (keyboardState[(int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_S] is 1)
                Camera.main.Move(0f, 0f, 1f);
            OnSetupMatrices?.Invoke();
        }

        private int oldX;
        private int oldY;

        public override void OnMouseDown(int buttonId)
        {
            if (buttonId is 1)
            {
                mouseLocked = true;
                Manipulators.selectedAxisId = 0;
            }
            else if (buttonId is 0)
            {
                mouseLocked = false;
                locPos = new Point(Gui.MousePosition.x - panel.Location.x, Gui.MousePosition.y - panel.Location.y);
                locPos = new Point(locPos.Value.x, locPos.Value.y - 29);
            }
            //	Console.WriteLine ("down");
        }

        public override void OnMouseUp(int buttonId)
        {
            if (AssetsView.isDragging)
            {
                var locPos = new Point(Gui.MousePosition.x - panel.Location.x, Gui.MousePosition.y - panel.Location.y);
                Camera.main.SetModelviewMatrix();
                var orig = Camera.main.entityObject.Position;
                var dir = (Camera.main.ScreenToWorld(locPos.x, locPos.y, panel.Size.x, panel.Size.y) - orig).Normalized();
                /*foreach (var asset in AssetsView.tree[AssetsView.whereDragStarted].SelectedChildren)
                {
                    Console.WriteLine(asset.Content.GetType());
                    (string name, string extension) = (ValueTuple<string, string>)asset.Content;
                    Pipeline.GetPipeline(extension).Import(name).PlaceIntoScene(null, orig + dir * Camera.main.ZFar * 0.1f);
                }*/
                AssetsView.isDragging = false;
                AssetsView.whereDragStarted = 0;
            }
            Manipulators.Reset();
        }

        public override void OnGlobalMouseMove(MouseMoveEventArgs evnt)
        {
            //Console.WriteLine("global mouse move " + mouseLocked);

            if (mouseLocked)
            {
                if (Manipulators.selectedAxisId == 0)
                {
                    Camera.main.Rotate(-evnt.XDelta, -evnt.YDelta, 0.3f);//maybe divide delta by fov?
                    OnSetupMatrices?.Invoke();
                }
                else//simple, precise, snapping
                {
                    if (evnt.XDelta != 0 || evnt.YDelta != 0)
                    {
                        var orig = Camera.main.entityObject.Position;
                        var winPos = Window.windows[attachedToWindow].Position;
                        var canvasPos = new System.Drawing.Point(evnt.Position.X - winPos.x, evnt.Position.Y - winPos.y);
                        var localMouse = new Point(Gui.MousePosition.x - panel.Location.x, Gui.MousePosition.y - panel.Location.y); ;
                        localMouse = new Point(localMouse.x, localMouse.y - 29);
                        var start = Camera.main.ScreenToWorld(localMouse.x, localMouse.y, panel.Size.x, panel.Size.y, 1);
                        var ray = new Ray(orig, (start - orig).Normalized());

                        /* foreach (var selected in SceneStructureView.tree.SelectedChildren)
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
                         }*/
                    }
                }
            }
            oldX = evnt.X;
            oldY = evnt.Y;
        }

        public override void OnGlobalMouseUp(MouseButtonEventArgs evnt)
        {
            Console.WriteLine("global mouse happened");
            mouseLocked = false;
        }
    }

    internal class OrderByDistanceToCamera : IComparer<Vector3>
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