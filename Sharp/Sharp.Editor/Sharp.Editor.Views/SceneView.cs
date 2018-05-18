using System.Collections.Generic;
using System.Numerics;
using System;
using Squid;

//using PhysX;
using SharpAsset.Pipeline;
using System.Linq;

using Newtonsoft.Json;

namespace Sharp.Editor.Views
{
    public class SceneView : View
    {
        private int cell_size = 32;
        private int grid_size = 4096;

        public static List<Entity> entities = new List<Entity>();
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
            var eLight = new Entity();
            eLight.name = "Directional Light";
            eLight.Position = Camera.main.entityObject.Position;
            var light = eLight.AddComponent<Light>();
            eLight.Instatiate();
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
            OnSetupMatrices?.Invoke();
        }

        private void SceneView_Update(Control sender)
        {
            throw new NotImplementedException();
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
                    OnSetupMatrices?.Invoke();
                }
                else//simple, precise, snapping
                {
                    if (Squid.UI.MouseDelta.x != 0 || Squid.UI.MouseDelta.y != 0)
                    {
                        var orig = Camera.main.entityObject.Position;
                        //var winPos = Window.windows[attachedToWindow].Position;
                        var localMouse = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
                        var start = Camera.main.ScreenToWorld(localMouse.x, localMouse.y, Size.x, Size.y, 1);

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
            OnSetupMatrices?.Invoke();
            Camera.main.AspectRatio = (float)Size.x / Size.y;
            Camera.main.SetProjectionMatrix();
            Camera.main.SetOrthoMatrix(Desktop.Size.x, Desktop.Size.y);
            Camera.main.frustum = new Frustum(Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix);
        }

        private void SceneView_KeyDown(Control sender, KeyEventArgs args)
        {
            if (Camera.main.moved)
            {
                Camera.main.moved = false;
                Camera.main.SetModelviewMatrix();
            }

            if (args.Key == Keys.Q)
                Camera.main.Move(0f, 1f, 0f);
            if (args.Key == Keys.E)
                Camera.main.Move(0f, -1f, 0f);
            if (args.Key == Keys.A)
                Camera.main.Move(-1f, 0f, 0f);
            if (args.Key == Keys.D)
                Camera.main.Move(1f, 0f, 0f);
            if (args.Key == Keys.W)
                Camera.main.Move(0f, 0f, -1f);
            if (args.Key == Keys.S)
                Camera.main.Move(0f, 0f, 1f);

            OnSetupMatrices?.Invoke();
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
            }
        }

        private void Panel_Drop(Control sender, DragDropEventArgs e)
        {
            var locPos = new Point(Squid.UI.MousePosition.x - Location.x, Squid.UI.MousePosition.y - Location.y);
            Camera.main.SetModelviewMatrix();
            var orig = Camera.main.entityObject.Position;
            if (e.Source.UserData is ValueTuple<string, string>[] entities)
                foreach (var asset in entities)
                {
                    (string name, string extension) = asset;
                    Pipeline.GetPipeline(extension).Import(name).PlaceIntoScene(null, orig + (Camera.main.ScreenToWorld(locPos.x, locPos.y, Size.x, Size.y) - orig).Normalize() * Camera.main.ZFar * 0.1f);
                }
        }

        protected override void DrawBefore()
        {
            //if (!IsVisible) return;
            //base.Render();
            MainWindow.backendRenderer.Viewport(Location.x, Camera.main.height - (Location.y + Size.y), Size.x, Size.y);
            if (locPos.HasValue)
            {
                if (!PickTestForGizmo())
                    PickTestForObject();
                locPos = null;
            }
            //MainWindow.backendRenderer.ClearBuffer();

            MainWindow.backendRenderer.SetStandardState();
            var projMat = Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;
            DrawHelper.DrawGrid(Color.White, Camera.main.entityObject.Position, cell_size, grid_size, ref projMat);

            OnRenderFrame?.Invoke();

            if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
            {
                //foreach (var selected in SceneStructureView.tree.SelectedChildren)
                {
                    Matrix4x4.Decompose(globalMode ? entity.ModelMatrix.Inverted() : entity.ModelMatrix, out _, out var rot, out var trans);
                    var mvpMat = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(trans) * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;//TODO: check if properly hit, probably trans first, quat later

                    MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);
                    MainWindow.backendRenderer.ClearDepth();
                    Manipulators.DrawCombinedGizmos(entity);
                    MainEditorView.editorBackendRenderer.UnloadMatrix();
                    /* var obj = JsonConvert.SerializeObject(SceneView.entities);
                     Console.WriteLine(obj);
                     var obj1 = JsonConvert.DeserializeObject<List<Entity>>(obj);
                     obj1[0].OnTransformChanged();*/
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
            MainWindow.backendRenderer.Viewport(0, 0, Desktop.Size.x, Desktop.Size.y);
        }

        private bool PickTestForGizmo()
        {
            //MainWindow.backendRenderer.ClearBuffer();
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
                if (SceneStructureView.tree.SelectedNode?.UserData is Entity entity)
                {
                    Matrix4x4.Decompose(globalMode ? entity.ModelMatrix.Inverted() : entity.ModelMatrix, out _, out var rot, out var trans);
                    var mvpMat = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(trans) * Camera.main.ModelViewMatrix * Camera.main.ProjectionMatrix;

                    MainEditorView.editorBackendRenderer.LoadMatrix(ref mvpMat);

                    Manipulators.DrawCombinedGizmos(entity, xColor, yColor, zColor, xRotColor, yRotColor, zRotColor, xScaleColor, yScaleColor, zScaleColor);

                    MainEditorView.editorBackendRenderer.UnloadMatrix();
                }
                MainWindow.backendRenderer.FinishCommands();
                if (locPos.HasValue)
                {
                    var pixel = MainWindow.backendRenderer.ReadPixels(Squid.UI.MousePosition.x, Camera.main.height - Squid.UI.MousePosition.y - 1 /*locPos.Value.y - 64*/, 1, 1);
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
            var end = Camera.main.ScreenToWorld(locPos.Value.x, locPos.Value.y, Size.x, Size.y);
            ;
            var ray = new Ray(orig, (end - orig).Normalize());
            var hitList = new SortedList<Vector3, int>(new OrderByDistanceToCamera());
            foreach (var ent in entities)
            {
                var render = ent.GetComponent<MeshRenderer>();
                Vector3 hitPoint = Vector3.Zero;
                if (render != null && render.mesh.bounds.Intersect(ray, ent.ModelMatrix, out hitPoint))
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
                SceneStructureView.tree.SelectedNode = null;
                SceneStructureView.tree.Nodes.Find((item) => item.UserData == entity).IsSelected = true;
            }
        }

        private int oldX;
        private int oldY;
    }

    internal class OrderByDistanceToCamera : IComparer<Vector3>
    {
        public int Compare(Vector3 x, Vector3 y)
        {
            var xDistance = (x - Camera.main.entityObject.Position).Length();
            var yDistance = (y - Camera.main.entityObject.Position).Length();
            if (xDistance > yDistance) return 1;
            else if (xDistance < yDistance) return -1;
            else return 0;
        }
    }
}