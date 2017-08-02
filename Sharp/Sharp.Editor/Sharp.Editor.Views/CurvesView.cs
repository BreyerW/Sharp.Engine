using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using Sharp.Editor.UI.Property;
using TupleExtensions;
using Squid;

namespace Sharp.Editor.Views
{
    public class CurvesView/*<T> where T:PropertyDrawer<Curve or Curve[]>*/ : View
    {
        private static Color kGridMinorColorDark = new Color(0f, 0f, 0f, 0.1f);
        private static Color kGridMajorColorDark = new Color(73, 73, 73, 120);
        private Dictionary<Vector2, Keyframe>[] clickedKeyframe = new Dictionary<Vector2, Keyframe>[] { new Dictionary<Vector2, Keyframe>(), new Dictionary<Vector2, Keyframe>() };

        private TangentDirection tangentSide = TangentDirection.None;
        private int showDrag = -1;
        private int editAxis = -1;
        private string typedNumber = string.Empty;
        public static RegionDrawer drawer;

        private static HashSet<Keyframe>[] tmpSavedKeyfr = new HashSet<Keyframe>[]
        {
        new HashSet<Keyframe>(),new HashSet<Keyframe>()
        };

        private (float x, float y, float width, float height) swatchArea;
        private Keyframe selectedTan;
        private bool isDraggingRegion = false;
        private Vector2 scale = Vector2.One;
        private Vector2 translation;

        private CurveSettings curveSettings = new CurveSettings();
        private static TickHandler hTicks = new TickHandler();
        private static TickHandler vTicks = new TickHandler();
        private (float x, float y, float width, float height) mainArea;

        private int tracking = -1;
        private Menu editMenu;
        private Menu menu;
        private Label badge;

        public (float x, float y, float width, float height) shownArea
        {
            get
            {
                return (-translation.X / scale.X, -(translation.Y - mainArea.height) / scale.Y, mainArea.width / scale.X, mainArea.height / -scale.Y);
            }
        }

        public CurvesView(uint attachToWindow) : base(attachToWindow)
        {
            float[] tickModulos = new float[]
          {
            1E-07f,
            5E-07f,
            1E-06f,
            5E-06f,
            1E-05f,
            5E-05f,
            0.0001f,
            0.0005f,
            0.001f,
            0.005f,
            0.01f,
            0.05f,
            0.1f,
            0.5f,
            1,
            5,
            10,
            50,
            100,
            500,
            1000,
            5000,
            10000,
            50000,
            100000,
            500000,
            1000000,
            5000000,
            1E+07f
          };
            hTicks.SetTickModulos(tickModulos);

            vTicks.SetTickModulos(tickModulos);
            curveSettings.vTickStyle.labelColor = new Color(0, 0, 0, 0.32f);
            curveSettings.vTickStyle.distLabel = 20;
            curveSettings.vTickStyle.centerLabel = true;

            curveSettings.hTickStyle.labelColor = new Color(0, 0, 0, 0.32f);
            curveSettings.hTickStyle.distLabel = 30;
            curveSettings.hTickStyle.centerLabel = true;

            MouseDown += Panel_MouseDown;

            menu = new Menu();
            menu.Style = "";
            menu.Align = Alignment.TopRight;
            menu.Size = new Point(0, 0);
            var addButton = menu.AddItem("Add key");
            addButton.Name = "add";
            addButton.MouseClick += AddButton_MouseClick;

            var trackButton = menu.AddItem("Track curve");
            trackButton.Name = "track";
            trackButton.MouseClick += TrackButton_MouseClick;

            editMenu = new Menu();
            editMenu.Style = "";
            editMenu.Align = Alignment.TopRight;
            editMenu.Size = new Point(0, 0);

            badge = new Label();
            badge.AutoEllipsis = false;
            badge.AutoSize = AutoSize.Horizontal;
            badge.NoEvents = true;
            badge.IsVisible = false;

            Childs.Add(badge);
            Childs.Add(menu);
            Childs.Add(editMenu);
            KeyDown += CurvesView_KeyDown;
            Button.Text = "Curves Inspector";
            Squid.UI.MouseMove += UI_MouseMove;
            Squid.UI.MouseUp += UI_MouseUp;
            badge.BringToFront();
        }

        private void UI_MouseUp(Control sender, Squid.MouseEventArgs args)
        {
            showDrag = -1;
            tangentSide = TangentDirection.None;
            badge.IsVisible = false;
            isDraggingRegion = false;
            if (clickedKeyframe[0].Count + clickedKeyframe[1].Count > 1)
            {
                clickedKeyframe[0].Clear();
                clickedKeyframe[1].Clear();
            }
            if (!delayClear) menu.IsVisible = false;
        }

        private void UI_MouseMove(Control sender, Squid.MouseEventArgs args)
        {
            if (!Window.windows.Contains(attachedToWindow)) return;
            var mousePos = new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
            if (tracking > -1)
            {
                var x = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation).X;
                var keyInCV = new Vector2(x, drawer.Value[tracking].Evaluate(x));
                CheckIfOutsideArea(ref keyInCV);
                var keyInVS = RegionDrawer.CurveToViewSpace(keyInCV, scale, translation);

                badge.Position = new Point((int)keyInVS.X, (int)keyInVS.Y);
                badge.Text = $"{x:F3}, {drawer.Value[tracking].Evaluate(x):F3}";
                // badge.SizeToContents();
                badge.IsVisible = true;
                return;
            }
            if (showDrag is -1) return;
            int l = 0;

            while (l < 2)
            {
                foreach (var (key, value) in clickedKeyframe[l].ToArray())
                {
                    var keyframePos = new Vector2(value.time, value.value);
                    var index = Array.FindIndex(drawer.Value[l].keys, keyframe => value.Equals(keyframe));
                    if (tangentSide == TangentDirection.None)
                    {
                        var newPos = !isDraggingRegion ? RegionDrawer.ViewToCurveSpace(mousePos, scale, translation) + key :
                                 new Vector2(value.time, RegionDrawer.ViewToCurveSpace(mousePos, scale, translation).Y + key.Y);

                        CheckIfOutsideArea(ref newPos);
                        var newKeyFr = new Keyframe(newPos.X, newPos.Y, value.inTangent, value.outTangent);
                        newKeyFr.tangentMode = value.tangentMode;

                        if (index < 0)
                        {
                            showDrag = drawer.Value[l].AddKey(ref newKeyFr);
                            if (showDrag < 0)
                            {
                                showDrag = 0;
                                continue;
                            }
                        }
                        else
                            showDrag = drawer.Value[l].MoveKey(index, ref newKeyFr);

                        while (showDrag == -1)
                        {
                            showDrag = Squid.UI.MouseDelta.x < 0 ? index - 1 : index;
                            if (!clickedKeyframe[l].ContainsValue(drawer.Value[l].keys[showDrag]) && !tmpSavedKeyfr[l].Contains(drawer.Value[l].keys[showDrag]))
                                tmpSavedKeyfr[l].Add(drawer.Value[l].keys[showDrag]);
                            drawer.Value[l].RemoveKey(showDrag);
                            showDrag = drawer.Value[l].AddKey(ref newKeyFr);
                        }

                        if (showDrag != -1)
                        {
                            CurveUtility.UpdateTangentsFromMode(drawer.Value[l], showDrag);
                            clickedKeyframe[l][key] = drawer.Value[l].keys[showDrag];
                        }
                        if (value.Equals(selectedTan))
                            selectedTan = clickedKeyframe[l][key];
                        if (!isDraggingRegion)
                        {
                            var keyInVS = RegionDrawer.CurveToViewSpace(newPos, scale, translation);
                            badge.Position = new Point((int)(keyInVS.X + 5f), (int)(keyInVS.Y - 15f));
                            badge.Text = $"{selectedTan.time:F3}, {selectedTan.value:F3}";
                            /*  badge.SizeToContents();*/
                            badge.IsVisible = true;
                        }
                    }
                    if (selectedTan.Equals(value) && tangentSide != TangentDirection.None)
                    {
                        var mousePosCS = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation);
                        var tmpKeyframe = value;
                        var tmpId = clickedKeyframe[l].First((item) =>
                        {
                            return item.Value.Equals(value);
                        }).Key;
                        if (tangentSide == TangentDirection.Right)
                        {
                            Vector2 vector2 = mousePosCS - keyframePos;
                            if (vector2.X < -0.0001f)
                            {
                                tmpKeyframe.inTangent = vector2.Y / vector2.X;
                            }
                            else
                            {
                                tmpKeyframe.inTangent = float.PositiveInfinity;
                            }
                            CurveUtility.SetKeyTangentMode(ref tmpKeyframe, 0, TangentMode.Editable);
                            if (!CurveUtility.GetKeyBroken(tmpKeyframe))
                            {
                                tmpKeyframe.outTangent = tmpKeyframe.inTangent;
                                CurveUtility.SetKeyTangentMode(ref tmpKeyframe, 1, TangentMode.Editable);
                            }
                        }
                        else
                        {
                            Vector2 vector = mousePosCS - keyframePos;

                            if (vector.X > 0.0001f)
                            {
                                tmpKeyframe.outTangent = vector.Y / vector.X;
                            }
                            else
                            {
                                tmpKeyframe.outTangent = float.PositiveInfinity;
                            }
                            CurveUtility.SetKeyTangentMode(ref tmpKeyframe, 1, TangentMode.Editable);
                            if (!CurveUtility.GetKeyBroken(tmpKeyframe))
                            {
                                tmpKeyframe.inTangent = tmpKeyframe.outTangent;
                                CurveUtility.SetKeyTangentMode(ref tmpKeyframe, 0, TangentMode.Editable);
                            }
                        }
                        drawer.Value[l].MoveKey(index, ref tmpKeyframe);
                        CurveUtility.UpdateTangentsFromModeSurrounding(drawer.Value[l], index);
                        clickedKeyframe[l][tmpId] = drawer.Value[l].keys[index];
                        selectedTan = drawer.Value[l].keys[index];
                    }
                }
                l++;
            }
            Squid.UI.isDirty = true;
            //showDrag = -1;
            editAxis = -1;
        }

        private void CurvesView_KeyDown(Control sender, KeyEventArgs args)
        {
            if (args.Key == Keys.ESCAPE)
            {
                tracking = -1;
                badge.IsVisible = false;
            }
        }

        private void TrackButton_MouseClick(Control sender, Squid.MouseEventArgs args)
        {
            tracking = (int)sender.UserData;
        }

        private void AddButton_MouseClick(Control sender, Squid.MouseEventArgs args)
        {
            object[] data = sender.UserData as object[];
            int curveId = (int)data[0];
            float time = (float)data[1];
            var startAngle = EvaluateCurveDeltaSlow(time, curveId);
            var newPoint = new Vector2(time, drawer.Value[curveId].Evaluate(time));
            CheckIfOutsideArea(ref newPoint);
            var newkeyfr = new Keyframe(newPoint.X, newPoint.Y, startAngle, startAngle);
            var keyId = drawer.Value[curveId].AddKey(ref newkeyfr);
            clickedKeyframe[0].Clear();
            clickedKeyframe[1].Clear();
            clickedKeyframe[curveId].Add(Vector2.Zero, drawer.Value[curveId].keys[keyId]);
            Squid.UI.isDirty = true;
        }

        private void Childs_BeforeItemAdded(object sender, ListEventArgs<Control> e)
        {
            e.Item.MouseClick += Dropdown_MouseClick;
        }

        private void Dropdown_MouseClick(Control sender, Squid.MouseEventArgs args)
        {
            editMenu.Close();
            menu.Close();
            showDrag = -1;
        }

        private void DelButton_MouseClick(Control sender, Squid.MouseEventArgs args)
        {
            (int curveId, int keyId)[] data = sender.UserData as (int, int)[];
            foreach (var toDelete in data)
                drawer.Value[toDelete.curveId].RemoveKey(toDelete.keyId);
            Squid.UI.isDirty = true;
        }

        private void Panel_MouseDown(Control sender, Squid.MouseEventArgs args)
        {
            var mousePos = new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
            var pos = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation);
            var rightClick = args.Button is 1;
            if (clickedKeyframe[0].Count + clickedKeyframe[1].Count == 1)
            {
                clickedKeyframe[0].Clear();
                clickedKeyframe[1].Clear();
            }
            bool checkMousePos = Math.Abs(drawer.Value[0].Evaluate(pos.X) - pos.Y) < 0.25f;
            bool checkMousePos1 = Math.Abs(drawer.Value[1].Evaluate(pos.X) - pos.Y) < 0.25f;
            var l = 0;
            var point = new Vector2[2];

            while (l < 2)
            {
                foreach (var item in drawer.Value[l].keys)
                {
                    var keyfrPos = new Vector2(item.time, item.value);
                    var keyfrPosSP = RegionDrawer.CurveToViewSpace(keyfrPos, scale, translation);

                    point[0] = RegionDrawer.CurveToViewSpace(keyfrPos.RotateAroundPivot(keyfrPos + new Vector2(1, 0), new Vector3((float)Math.Atan(item.outTangent), 0, 0)), scale, translation);
                    point[1] = RegionDrawer.CurveToViewSpace(keyfrPos.RotateAroundPivot(keyfrPos + new Vector2(-1, 0), new Vector3((float)Math.Atan(item.inTangent), 0, 0)), scale, translation);

                    var dir = point[0] - keyfrPosSP;
                    point[0] = keyfrPosSP + dir.Normalized() * 50;
                    dir = point[1] - keyfrPosSP;
                    point[1] = keyfrPosSP + dir.Normalized() * 50;

                    if (Math.Abs((keyfrPosSP - mousePos).LengthSquared) < 11)
                    {
                        (int, int)[] data;
                        selectedTan = item;
                        if (clickedKeyframe[l].Count == 0)
                        {
                            clickedKeyframe[l].Add(keyfrPos - pos, item);
                            data = new(int, int)[] { (l, Array.IndexOf(drawer.Value[l].keys, item)) };
                        }
                        else
                        {
                            data = new(int, int)[clickedKeyframe[0].Count + clickedKeyframe[1].Count];
                            var tmpl = 0;
                            while (tmpl < 2)
                            {
                                var tmpList = clickedKeyframe[tmpl].Values.ToList();
                                clickedKeyframe[tmpl].Clear();
                                foreach (var (key, selectedKeyfr) in tmpList.WithIndexes())
                                {
                                    var tmpkeyfrPos = new Vector2(selectedKeyfr.time, selectedKeyfr.value);
                                    data[key] = (tmpl, Array.IndexOf(drawer.Value[l].keys, selectedKeyfr));
                                    clickedKeyframe[tmpl][tmpkeyfrPos - pos] = selectedKeyfr;
                                }
                                tmpl++;
                            }
                        }
                        if (rightClick)
                        {
                            CurveMenuManager.selectedKeyfr = clickedKeyframe;
                            CurveMenuManager.updateSelected = UpdateClickedKeyfr;
                            editMenu.Frame.Controls.Clear();

                            editMenu.AddItem("Edit key");//edit with keyboard
                            editMenu.AddItem("Edit tangents");
                            var delButton = editMenu.AddItem("Delete Key");
                            delButton.Name = "delete";
                            delButton.MouseClick += DelButton_MouseClick;
                            delButton.Text = "Delete Key" + (clickedKeyframe[0].Count + clickedKeyframe[1].Count > 1 ? "s" : "");
                            delButton.UserData = data;
                            //var delButton = editMenu.Dropdown.GetControl("delete") as Button;

                            CurveMenuManager.AddTangentMenuItems(editMenu, drawer.Value);

                            editMenu.Position = new Point((int)mousePos.X - Location.x, (int)mousePos.Y - Location.y);
                            editMenu.IsVisible = true;
                            editMenu.Open();
                            delayClear = true;

                            return;
                        }
                        else
                        {
                            badge.IsVisible = true;
                            badge.Position = new Point((int)(keyfrPosSP.X + 5f), (int)(keyfrPosSP.Y - 15f));
                            showDrag = Array.IndexOf(drawer.Value[l].keys, item);
                            return;
                        }
                    }
                    else if (Math.Abs((point[0] - mousePos).LengthSquared) < 11 && !rightClick)
                    {
                        tangentSide = TangentDirection.Left;
                        selectedTan = item;
                        clickedKeyframe[l][keyfrPos - pos] = item;
                        showDrag = Array.IndexOf(drawer.Value[l].keys, item);
                        return;
                    }
                    else if (Math.Abs((point[1] - mousePos).LengthSquared) < 11 && !rightClick)
                    {
                        tangentSide = TangentDirection.Right;
                        selectedTan = item;
                        clickedKeyframe[l][keyfrPos - pos] = item;
                        showDrag = Array.IndexOf(drawer.Value[l].keys, item);
                        return;
                    }
                }
                l++;
            }
            if ((checkMousePos || checkMousePos1) && rightClick)
            {
                var addButton = menu.Dropdown.GetControl("add");
                addButton.UserData = new object[]
                 {
                 checkMousePos ? 0 : 1,
                 pos.X,
                 };
                var trackButton = menu.Dropdown.GetControl("track");
                trackButton.UserData = checkMousePos ? 0 : 1;

                menu.Position = new Point((int)mousePos.X - Location.x, (int)mousePos.Y - Location.y);
                menu.IsVisible = true;
                menu.Open();

                delayClear = true;
                return;
            }
            else if (showDrag != 0)
            {// clickedKeyframe[0].Clear();
             //clickedKeyframe[1].Clear();
                Vector2 keyfrPos;
                l = 0;
                var rounds = 2;
                isDraggingRegion = true;
                showDrag = 0;
                if (checkMousePos)
                {
                    rounds = 1;
                }
                else if (checkMousePos1)
                {
                    l = 1;
                }
                else if ((drawer.Value[0].Evaluate(pos.X) < pos.Y && drawer.Value[1].Evaluate(pos.X) < pos.Y) || (drawer.Value[0].Evaluate(pos.X) > pos.Y && drawer.Value[1].Evaluate(pos.X) > pos.Y))
                {
                    isDraggingRegion = false;
                    rounds = 0;
                }
                while (l < rounds)
                {
                    foreach (var selectedKeyfr in drawer.Value[l].keys)
                    {
                        keyfrPos = new Vector2(selectedKeyfr.time, selectedKeyfr.value);
                        clickedKeyframe[l][keyfrPos - pos] = selectedKeyfr;
                    }
                    l++;
                }
                return;
            }
            //showDrag = 0;
            isDraggingRegion = false;
            showDrag = -1;
            delayClear = false;
        }

        private void OnFocus()
        {
            //SelectionHandler.OnSelectionDone = PerformSelectionAction;
            //SelectionHandler.OnSelectionDrag = PerformSelectionAction;
        }

        private void OnEnable()
        {
            //position = (position.x, position.y, 1000, 400);
            //minSize = new Vector2(minSize.x, 200);
        }

        private void OnGUI()
        {
            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            var size = Window.windows[attachedToWindow].Size;
            mainArea = (0, 29, size.width, size.height);

            swatchArea = (40, 40 + 29, Size.x - 80, size.height - 80 - 29);
            var max = drawer.curvesRange.height + (drawer.curvesRange.y < 0 ? drawer.curvesRange.y : 0);
            var maxY = drawer.curvesRange.width + (drawer.curvesRange.x < 0 ? drawer.curvesRange.x : 0);
            scale = new Vector2(swatchArea.width / (drawer.curvesRange.width), -swatchArea.height / (drawer.curvesRange.height));//max-drawer.curvesRange.y
            translation = new Vector2(-drawer.curvesRange.x * scale.X + swatchArea.x, swatchArea.height - drawer.curvesRange.y * scale.Y + swatchArea.y);
            Color c = new Color(drawer.curveColor.R, drawer.curveColor.G, drawer.curveColor.B, (byte)(drawer.curveColor.A * 0.75f));
            var mat = mainView.camera.OrthoLeftBottomMatrix;
            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);

            var direction = new Vector2(5, 5);
            for (int j = 0; j < 2; j++)
            {
                c = PrepareColorForCurve(c, j);
                foreach (var (key, keyframe) in drawer.Value[j].keys.WithIndexes())
                {
                    var screenPos = RegionDrawer.CurveToViewSpace(new Vector2(keyframe.time, keyframe.value), scale, translation);

                    MainEditorView.editorBackendRenderer.DrawDiamond(screenPos, direction, ref c.R);

                    var keyframePos = new Vector2(keyframe.time, keyframe.value);
                    var posX = RegionDrawer.CurveToViewSpace(keyframePos, scale, translation).X;
                    var posY = RegionDrawer.CurveToViewSpace(keyframePos, scale, translation).Y;
                    if (!isDraggingRegion && clickedKeyframe[j].ContainsValue(keyframe))
                    {
                        var handlePos = new Vector2(posX, posY);
                        var point = new Vector2[2];
                        var col = new Color(190, 200, 200, 200);
                        if (false/*base.settings.useFocusColors && !hasFocus*/)
                        {
                            col = new Color((int)(col.A * 0.5f), (int)(col.R * 0.5f), (int)(col.G * 0.5f), (int)(col.B * 0.5f));
                        }
                        var color2 = Color.Lerp(drawer.curveColor, Color.White, 0.2f);
                        if (key < drawer.Value[j].keys.Length - 1 && CurveUtility.GetKeyTangentMode(ref drawer.Value[j].keys[key], 1) == TangentMode.Editable)
                        {
                            point[0] = RegionDrawer.CurveToViewSpace(keyframePos.RotateAroundPivot(keyframePos + new Vector2(1, 0), new Vector3((float)Math.Atan(keyframe.outTangent), 0, 0)), scale, translation);
                            var dir = point[0] - handlePos;
                            point[0] = handlePos + dir.Normalized() * 50;
                            MainEditorView.editorBackendRenderer.DrawLine(handlePos.X, handlePos.Y, 0, point[0].X, point[0].Y, 0, ref col.R);
                            MainEditorView.editorBackendRenderer.DrawDiamond(point[0], direction, ref col.R);
                        }
                        if (key > 0 && CurveUtility.GetKeyTangentMode(ref drawer.Value[j].keys[key], 0) == TangentMode.Editable)
                        {
                            point[1] = RegionDrawer.CurveToViewSpace(keyframePos.RotateAroundPivot(keyframePos + new Vector2(-1, 0), new Vector3((float)Math.Atan(keyframe.inTangent), 0, 0)), scale, translation);
                            var dir = point[1] - handlePos;
                            point[1] = handlePos + dir.Normalized() * 50;
                            MainEditorView.editorBackendRenderer.DrawLine(handlePos.X, handlePos.Y, 0, point[1].X, point[1].Y, 0, ref col.R);
                            MainEditorView.editorBackendRenderer.DrawDiamond(point[1], direction, ref col.R);
                        }
                    }
                }
            }

            for (int j = 0; j < 2; j++)
            {
                for (int i = 0; i < drawer.lines[j].Length - 1; i++)
                {
                    var start = RegionDrawer.CurveToViewSpace(drawer.lines[j][i], scale, translation);
                    var end = RegionDrawer.CurveToViewSpace(drawer.lines[j][i + 1], scale, translation);
                    MainEditorView.editorBackendRenderer.DrawLine(start.X, start.Y, 0, end.X, end.Y, 0, ref c.R);
                }
            }
            c = PrepareColorForRegion(drawer.curveColor);

            var array = new Vector3[drawer.region.Length];
            for (int i = 0; i < drawer.region.Length; i++)
            {
                array[i] = new Vector3(RegionDrawer.CurveToViewSpace(drawer.region[i], scale, translation));
            }
            MainWindow.backendRenderer.WriteDepth(false);
            GridGUI();
            MainEditorView.editorBackendRenderer.DrawFilledPolyline(3, 3, ref c.R, ref mat, ref array, false);
            MainWindow.backendRenderer.WriteDepth();
            MainEditorView.editorBackendRenderer.UnloadMatrix();
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
        }

        private void UpdateClickedKeyfr(Dictionary<Vector2, Keyframe>[] newSelected)
        {
            clickedKeyframe = newSelected;
        }

        private void CheckIfOutsideArea(ref Vector2 point)
        {
            if (point.X > drawer.curvesRange.width + drawer.curvesRange.x)
            {
                point.X = drawer.curvesRange.width + drawer.curvesRange.x;
            }
            else if (point.X < drawer.curvesRange.x)
            {
                point.X = drawer.curvesRange.x;
            }
            if (point.Y > drawer.curvesRange.height + drawer.curvesRange.y)
            {
                point.Y = drawer.curvesRange.height + drawer.curvesRange.y;
            }
            else if (point.Y < drawer.curvesRange.y)
            {
                point.Y = drawer.curvesRange.y;
            }
        }

        private Color PrepareColorForCurve(Color color, int curveId)
        {
            if (isDraggingRegion && clickedKeyframe[curveId].Count > 0 && clickedKeyframe[curveId == 0 ? 1 : 0].Count == 0)
                color = Color.Lerp(color, Color.White, 0.3f);
            else
            {
                if (false /*base.settings.useFocusColors && !hasFocus*/)
                {
                    color = new Color(color.R * 0.5f, color.G * 0.5f, color.B * 0.5f, 0.8f);
                }
            }
            return color;
        }

        private Color PrepareColorForRegion(Color color)
        {
            if (isDraggingRegion && clickedKeyframe[0].Count > 0 && clickedKeyframe[1].Count > 0)
            {
                color = Color.Lerp(color, Color.Black, 0.1f);
                color = new Color(color.R, color.G, color.B, (byte)(255 * 0.4f));
            }
            else
            {
                if (false /*base.settings.useFocusColors && !hasFocus*/)
                {
                    color = new Color(color.R * 0.4f, color.G * 0.4f, color.B * 0.4f, 0.1f);
                }
                else
                {
                    color = new Color(color.R, color.G, color.B, (byte)(255 * 0.4f));
                }
            }
            return color;
        }

        private void PerformSelectionAction((int x, int y, int width, int height) selectedArea)
        {
            var l = 0;
            while (l < 2)
            {
                var index = 0;
                clickedKeyframe[l].Clear();
                foreach (var item in drawer.Value[l].keys)
                {
                    var pos = RegionDrawer.CurveToViewSpace(new Vector2(item.time, item.value), scale, translation);
                    //if (selectedArea.Contains(pos))
                    {
                        //      clickedKeyframe[l].Add((pos - mousePosition) / 100, item);
                    }
                    index++;
                }
                l++;
            }
        }

        protected override void DrawBefore()
        {
            MainWindow.backendRenderer.ChangeShader();
            // MainWindow.backendRenderer.Clip(panel.Location.x, panel.Location.y, panel.Size.x, panel.Size.y);
            OnGUI();
        }

        private bool delayClear = true;

        //convert curve keys to basic gui? then use clicked/rightclicked etc events on them

        //void TraverseKeyframes(Rect swatchArea, Rect curvesRange)

        public void GridGUI()
        {
            Vector2 axisUiScalars = GetAxisUiScalars(null);
            hTicks.SetRanges(shownArea.x * axisUiScalars.X, (shownArea.x + shownArea.width) * axisUiScalars.X, swatchArea.x, swatchArea.x + swatchArea.width);
            vTicks.SetRanges(shownArea.y * axisUiScalars.Y, (shownArea.y + shownArea.height) * axisUiScalars.Y, swatchArea.y, swatchArea.y + swatchArea.height);

            hTicks.SetTickStrengths((float)curveSettings.hTickStyle.distMin, (float)curveSettings.hTickStyle.distFull, false);
            float num;
            float num2;
            var maxY = drawer.curvesRange.height + (drawer.curvesRange.y < 0 ? drawer.curvesRange.y : 0);
            var maxX = drawer.curvesRange.width + (drawer.curvesRange.x < 0 ? drawer.curvesRange.x : 0);
            if (curveSettings.hTickStyle.stubs)
            {
                num = shownArea.y;
                num2 = shownArea.y - 40 / scale.Y;
            }
            else
            {
                num = Math.Max(shownArea.y, drawer.curvesRange.y);
                num2 = Math.Min(shownArea.y + shownArea.height, maxY);
            }
            Color c;
            for (int i = 0; i < hTicks.tickLevels; i++)
            {
                float strengthOfLevel = hTicks.GetStrengthOfLevel(i);
                if (strengthOfLevel > 0)
                {
                    c = curveSettings.hTickStyle.color * new Color(1f, 1f, 1f, strengthOfLevel) * new Color(1f, 1f, 1f, 0.75f);
                    float[] ticksAtLevel = hTicks.GetTicksAtLevel(i, true);
                    for (int j = 0; j < ticksAtLevel.Length; j++)
                    {
                        ticksAtLevel[j] /= axisUiScalars.X;
                        if (ticksAtLevel[j] > drawer.curvesRange.x && ticksAtLevel[j] < maxX)
                        {
                            MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(ticksAtLevel[j], num), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(ticksAtLevel[j], num2), scale, translation)), ref c.R);
                        }
                    }
                }
            }
            c = curveSettings.hTickStyle.color * new Color(1f, 1f, 1f, 1f) * new Color(1, 1, 1, 0.75f);
            if (drawer.curvesRange.x != -float.PositiveInfinity)
            {
                MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(drawer.curvesRange.x, num), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(drawer.curvesRange.x, num2), scale, translation)), ref c.R);
            }
            if (maxX != float.PositiveInfinity)
            {
                MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(maxX, num), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(maxX, num2), scale, translation)), ref c.R);
            }
            vTicks.SetTickStrengths((float)curveSettings.vTickStyle.distMin, (float)curveSettings.vTickStyle.distFull, false);
            if (curveSettings.vTickStyle.stubs)
            {
                num = shownArea.x;
                num2 = shownArea.x + 40 / scale.X;
            }
            else
            {
                num = Math.Max(shownArea.x, drawer.curvesRange.x);
                num2 = Math.Min(shownArea.x + shownArea.width, maxX);
            }
            for (int k = 0; k < vTicks.tickLevels; k++)
            {
                float strengthOfLevel2 = vTicks.GetStrengthOfLevel(k);
                if (strengthOfLevel2 > 0)
                {
                    c = curveSettings.vTickStyle.color * new Color(1, 1, 1, strengthOfLevel2) * new Color(1, 1, 1, 0.75f);//Color.Lerp(Color.Transparent, curveSettings.vTickStyle.color, strengthOfLevel2) * new Color(1f, 1f, 1f, 0.75f);
                    float[] ticksAtLevel2 = vTicks.GetTicksAtLevel(k, true);
                    for (int l = 0; l < ticksAtLevel2.Length; l++)
                    {
                        ticksAtLevel2[l] /= axisUiScalars.Y;
                        if (ticksAtLevel2[l] > drawer.curvesRange.y && ticksAtLevel2[l] < maxY)
                        {
                            MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num, ticksAtLevel2[l]), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num2, ticksAtLevel2[l]), scale, translation)), ref c.R);
                        }
                    }
                }
            }
            c = curveSettings.vTickStyle.color * new Color(1f, 1f, 1f, 1f) * new Color(1, 1, 1, 0.75f);
            if (drawer.curvesRange.y != -float.PositiveInfinity)
            {
                MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num, drawer.curvesRange.y), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num2, drawer.curvesRange.y), scale, translation)), ref c.R);
            }
            if (maxY != float.PositiveInfinity)
            {
                MainEditorView.editorBackendRenderer.DrawLine(new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num, maxY), scale, translation)), new Vector3(RegionDrawer.CurveToViewSpace(new Vector2(num2, maxY), scale, translation)), ref c.R);
            }

            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            if (curveSettings.showAxisLabels)
            {
                if (curveSettings.hTickStyle.distLabel > 0 && axisUiScalars.X > 0)
                {
                    // curveSettings.hTickStyle.labelColor;
                    int levelWithMinSeparation = hTicks.GetLevelWithMinSeparation((float)curveSettings.hTickStyle.distLabel);
                    int numberOfDecimalsForMinimumDifference = MathHelper.Clamp(-(int)Math.Floor(Math.Log10(hTicks.GetPeriodOfLevel(levelWithMinSeparation))), 0, 15);
                    float[] ticksAtLevel3 = hTicks.GetTicksAtLevel(levelWithMinSeparation, false);
                    float[] array = (float[])ticksAtLevel3.Clone();

                    float y = (float)Math.Floor(mainArea.height);
                    for (int m = 0; m < ticksAtLevel3.Length; m++)
                    {
                        array[m] /= axisUiScalars.X;
                        if (array[m] >= drawer.curvesRange.x && array[m] <= maxX)
                        {
                            Vector2 vector = RegionDrawer.CurveToViewSpace(new Vector2(array[m], 0), scale, translation);
                            vector = new Vector2((float)Math.Floor(vector.X), y);
                            float num3 = ticksAtLevel3[m];
                            (float x, float y, float width, float height) position;
                            Alignment textAnchor;
                            if (curveSettings.hTickStyle.centerLabel)
                            {
                                textAnchor = Alignment.MiddleCenter;
                                position = (vector.X, vector.Y + curveSettings.hTickLabelOffset - 29, 1, 16);
                            }
                            else
                            {
                                textAnchor = Alignment.MiddleLeft;
                                position = (vector.X, vector.Y + curveSettings.hTickLabelOffset, 50, 16);
                            }
                            var alignment = AlignText(num3.ToString("n" + numberOfDecimalsForMinimumDifference) + curveSettings.hTickStyle.unit, new Point((int)position.x, (int)position.y), new Point((int)position.width, (int)position.height), textAnchor, Margin.Empty, 0);
                            Squid.UI.Renderer.DrawText(num3.ToString("n" + numberOfDecimalsForMinimumDifference) + curveSettings.hTickStyle.unit, alignment.x, alignment.y, (int)position.width, (int)position.height, 0, -1, 0);
                        }
                    }
                }
                if (curveSettings.vTickStyle.distLabel > 0 && axisUiScalars.Y > 0)
                {
                    //curveSettings.vTickStyle.labelColor;
                    int levelWithMinSeparation2 = vTicks.GetLevelWithMinSeparation((float)curveSettings.vTickStyle.distLabel);
                    float[] ticksAtLevel4 = vTicks.GetTicksAtLevel(levelWithMinSeparation2, false);
                    float[] array2 = (float[])ticksAtLevel4.Clone();

                    int numberOfDecimalsForMinimumDifference2 = MathHelper.Clamp(-(int)Math.Floor(Math.Log10(hTicks.GetPeriodOfLevel(levelWithMinSeparation2))), 0, 15); ;
                    string text = "n" + numberOfDecimalsForMinimumDifference2;
                    float width = 35;
                    if (!curveSettings.vTickStyle.stubs && ticksAtLevel4.Length > 1)
                    {
                        float num4 = ticksAtLevel4[1];
                        float num5 = ticksAtLevel4[ticksAtLevel4.Length - 1];
                        string text2 = num4.ToString(text) + curveSettings.vTickStyle.unit;
                        string text3 = num5.ToString(text) + curveSettings.vTickStyle.unit;
                    }
                    for (int n = 0; n < ticksAtLevel4.Length; n++)
                    {
                        array2[n] /= axisUiScalars.Y;
                        if (array2[n] >= drawer.curvesRange.y && array2[n] <= maxY)
                        {
                            Vector2 vector2 = RegionDrawer.CurveToViewSpace(new Vector2(0, array2[n]), scale, translation);
                            vector2 = new Vector2(vector2.X, (float)Math.Floor(vector2.Y));
                            float num6 = ticksAtLevel4[n];
                            (float x, float y, float width, float height) position2;
                            Alignment textAnchor = Alignment.MiddleRight;
                            if (curveSettings.vTickStyle.centerLabel)
                            {
                                position2 = (-5, vector2.Y - 8, swatchArea.x, 16);
                            }
                            else
                            {
                                textAnchor = Alignment.MiddleLeft;
                                position2 = (-5, vector2.Y - 8, width, 16);
                            }
                            var alignment = AlignText(num6.ToString(text) + curveSettings.vTickStyle.unit, new Point((int)position2.x, (int)position2.y), new Point((int)position2.width, (int)position2.height), textAnchor, Margin.Empty, 0);
                            Squid.UI.Renderer.DrawText(num6.ToString(text) + curveSettings.vTickStyle.unit, alignment.x, alignment.y, (int)position2.width, (int)position2.height, 0, -1, 0);
                        }
                    }
                }
            }
            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
        }

        public float EvaluateCurveDeltaSlow(float time, int curveId)
        {
            float num = 0.0001f;
            return (drawer.Value[curveId].Evaluate(time + num) - drawer.Value[curveId].Evaluate(time - num)) / (num * 2f);
        }

        private Vector2 GetAxisUiScalars(Curve[] curvesWithSameParameterSpace)
        {
            Vector2 result = new Vector2(-1, -1);
            if (drawer.Value.Length > 1)
            {
                bool flag = true;
                bool flag2 = true;
                Vector2 vector = Vector2.One;
                for (int i = 0; i < drawer.Value.Length; i++)
                {
                    Curve curveWrapper = drawer.Value[i];
                    Vector2 vector2 = Vector2.One;
                    if (i == 0)
                    {
                        vector = vector2;
                    }
                    else
                    {
                        if (Math.Abs(vector2.X - vector.X) > 1E-05f)
                        {
                            flag = false;
                        }
                        if (Math.Abs(vector2.Y - vector.Y) > 1E-05f)
                        {
                            flag2 = false;
                        }
                        vector = vector2;
                    }
                }
                if (flag)
                {
                    result.X = vector.X;
                }
                if (flag2)
                {
                    result.Y = vector.Y;
                }
            }
            return result;
        }
    }
}