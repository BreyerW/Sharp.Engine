using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using SharpAsset;
using SharpAsset.Pipeline;
using Squid;
using OpenTK.Graphics.OpenGL;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Sharp.Editor.Views
{
	public class CurvesView/*<T> where T:PropertyDrawer<Curve or Curve[]>*/ : View
	{
		private static Color kGridMinorColorDark = new Color(0f, 0f, 0f, 0.1f);
		private static Color kGridMajorColorDark = new Color(73, 73, 73, 120);

		private int showDrag = -1;
		private string typedNumber = string.Empty;
		public static RegionDrawer drawer;

		private (float x, float y, float width, float height) swatchArea;

		private int draggingCurveId = -1;

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

		private Control selectedControl;

		public (float x, float y, float width, float height) shownArea
		{
			get
			{
				return (-translation.X / scale.X, -(translation.Y - mainArea.height) / scale.Y, mainArea.width / scale.X, mainArea.height / -scale.Y);
			}
		}

		public CurvesView(uint attachToWindow, RegionDrawer drawer) : base(attachToWindow)
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
			
			AllowFocus = true;

			menu = new Menu(new Squid.Window());
			menu.Style = "";
			menu.Align = Alignment.TopRight;
			menu.Size = new Point(0, 0);

			var addButton = menu.AddItem("Add key");
			addButton.Name = "add";
			addButton.MouseClick += AddButton_MouseClick;

			var trackButton = menu.AddItem("Track curve");
			trackButton.Name = "track";
			trackButton.MouseClick += TrackButton_MouseClick;

			editMenu = new Menu(new Squid.Window());
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

			Button.Text = "Curves Inspector";
			Squid.UI.MouseMove += UI_MouseMove;
			Squid.UI.MouseUp += UI_MouseUp;

			Padding = new Margin(32, 33, 32, 32);
			badge.BringToFront();
			ref var curves = ref drawer.Value;
			Color c = new Color(drawer.curveColor.R, drawer.curveColor.G, drawer.curveColor.B, (byte)(drawer.curveColor.A * 0.75f));
			foreach (var j in ..curves.Length)
			{
				c = PrepareColorForCurve(c, j);

				foreach (var (key, keyframe) in curves[j].keys.Indexed())
				{
					AddButtonsForNewKey(keyframe, j, ref c);
				}
			}
			Update += CurvesView_Update;
			MouseUp += Panel_MouseUp;
			MouseDown += CurvesView_MouseDown;
			SizeChanged += CurvesView_SizeChanged;
			KeyUp += CurvesView_KeyDown;
		}
		private void CurvesView_Update(Control sender)
		{
			ref var curves = ref drawer.Value;

			Color c = new Color(drawer.curveColor.R, drawer.curveColor.G, drawer.curveColor.B, (byte)(drawer.curveColor.A * 0.75f));
			foreach (var j in ..curves.Length)
			{
				c = PrepareColorForCurve(c, j);
				var draggableButtons = Childs.FindAll(
					(item) =>
					{
						return item is { UserData: (_, int id, _, _) } && id == j;
					});
				if (draggableButtons.Count != curves[j].keys.Length)
				{
					foreach (var button in draggableButtons)
					{
						(_, _, var inTan, var outTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)button.UserData;
						Childs.Remove(button);
						Childs.Remove(inTan);
						Childs.Remove(outTan);
					}
					foreach (var (key, keyframe) in curves[j].keys.Indexed())
					{
						AddButtonsForNewKey(keyframe, j, ref c);
					}
				}
				else
				{
					var keyId = 0;
					foreach (var button in draggableButtons)
					{
						var keyframe=curves[j].keys[keyId];
						var keyframePos = new Vector2(keyframe.time, keyframe.value);
						var point = RegionDrawer.CurveToViewSpace(keyframePos, scale, translation);
						(_, _, var inTan, var outTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)button.UserData;
						ChangePositionWithoutEvent(button, new Point((int)point.X - 10, (int)point.Y - Location.y - 6));
						var outPos = new Point(button.Position.x + 6, button.Position.y + 6) + (Point)outTan.UserData;
						ChangePositionWithoutEvent(outTan, new Point(outPos.x - 6, outPos.y - 6));
						var inPos = new Point(button.Position.x + 6, button.Position.y + 6) + (Point)inTan.UserData;
						ChangePositionWithoutEvent(inTan, new Point(inPos.x - 6, inPos.y - 6));
						keyId++;
					}
				}
			}
		}

		private void CurvesView_MouseDown(Control sender, MouseEventArgs args)
		{
			var mousePos = new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
			var pos = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation);
			var rightClick = args.Button is 1;
			if (rightClick) return;
			var checkMousePos = new bool[drawer.Value.Length];
			var checkIfOutsideRegion = new bool[drawer.Value.Length / 2];
			ref var curves = ref drawer.Value;
			foreach (var (id, curve) in curves.Indexed())
			{
				checkMousePos[id] = Math.Abs(curve.Evaluate(pos.X) - pos.Y) < 1f;
				if ((id & 1) is 0)
					checkIfOutsideRegion[id / 2] = (curves[id / 2].Evaluate(pos.X) < pos.Y && curves[id / 2 + 1].Evaluate(pos.X) < pos.Y) || (curves[id / 2].Evaluate(pos.X) > pos.Y && curves[id / 2 + 1].Evaluate(pos.X) > pos.Y);
			}
			foreach (var child in Childs)
			{
				if (child.Name != "inTan" && child.Name != "outTan" && child is DraggableButton button)
				{
					var (_, l, _, _) = (ValueTuple<float, int, DraggableButton, DraggableButton>)button.UserData;
					if (checkIfOutsideRegion[l / 2])
						continue;

					if (checkMousePos[l])
					{
						draggingCurveId = l;
						button.StartDrag();
						Desktop.CurrentCursor = CursorNames.SizeNS;
					}
					else if (!checkMousePos[(l & 1) is 0 ? l + 1 : l - 1])
					{
						draggingCurveId = -(l / 2) - 2;
						button.StartDrag();
						Desktop.CurrentCursor = CursorNames.SizeNS;
					}
				}
			}
		}

		private void AddButtonsForNewKey(Keyframe keyframe, int curveId, ref Color c)
		{
			var keyframePos = new Vector2(keyframe.time, keyframe.value);
			var point = RegionDrawer.CurveToViewSpace(keyframePos, scale, translation);
			var draggableButton = new DraggableButton();
			draggableButton.ConfineToParent = true;
			draggableButton.Tint = (int)c.PackedValue;
			draggableButton.Size = new Point(11, 11);
			//draggableButton.AllowFocus = true;

			draggableButton.PositionChanged += DraggableButton_PositionChanged;
			draggableButton.MouseDown += DraggableButton_MouseDown;
			var col = c;// new Color(190, 200, 200, 200);
			if (false/*base.settings.useFocusColors && !hasFocus*/)
			{
				col = new Color((int)(col.A * 0.5f), (int)(col.R * 0.5f), (int)(col.G * 0.5f), (int)(col.B * 0.5f));
			}

			var outTanButton = new DraggableButton();
			outTanButton.Tint = (int)col.PackedValue;
			outTanButton.Size = new Point(11, 11);
			outTanButton.IsVisible = false;
			outTanButton.Name = "outTan";
			CalcTangentForButton(outTanButton, draggableButton, ref keyframe);
			outTanButton.PositionChanged += TanButton_PositionChanged;

			var inTanButton = new DraggableButton();
			inTanButton.Tint = (int)col.PackedValue;
			inTanButton.Size = new Point(11, 11);
			inTanButton.IsVisible = false;
			inTanButton.Name = "inTan";
			CalcTangentForButton(inTanButton, draggableButton, ref keyframe);

			inTanButton.PositionChanged += TanButton_PositionChanged;
			draggableButton.UserData = ((keyframe.time, curveId, outTanButton, inTanButton));

			ChangePositionWithoutEvent(draggableButton, new Point((int)point.X - 10, (int)point.Y - Location.y - 6));
			var outPos = new Point(draggableButton.Position.x + 6, draggableButton.Position.y + 6) + (Point)outTanButton.UserData;
			ChangePositionWithoutEvent(outTanButton, new Point(outPos.x - 6, outPos.y - 6));
			var inPos = new Point(draggableButton.Position.x + 6, draggableButton.Position.y + 6) + (Point)inTanButton.UserData;
			ChangePositionWithoutEvent(inTanButton, new Point(inPos.x - 6, inPos.y - 6));
			Childs.Add(outTanButton);
			Childs.Add(inTanButton);
			Childs.Add(draggableButton);
		}

		private void TanButton_PositionChanged(Control sender)
		{
			var mousePosCS = RegionDrawer.ViewToCurveSpace(new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y), scale, translation);
			var (time, l, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)selectedControl.UserData;
			var key = FindKeyframe(time, l);
			ref var curves = ref drawer.Value;
			var value = curves[l].keys[key];
			var keyframePos = new Vector2(value.time, value.value);
			if (sender.Name is "inTan")
			{
				Vector2 vector2 = mousePosCS - keyframePos;
				if (vector2.X < -0.0001f)
				{
					value.inTangent = vector2.Y / vector2.X;
				}
				else
				{
					value.inTangent = float.PositiveInfinity;
				}

				CurveUtility.SetKeyTangentMode(ref value, 0, TangentMode.Editable);
				CalcTangentForButton(inTan, selectedControl, ref value);
				if (!CurveUtility.GetKeyBroken(value))
				{
					value.outTangent = value.inTangent;
					CurveUtility.SetKeyTangentMode(ref value, 1, TangentMode.Editable);
					CalcTangentForButton(outTan, selectedControl, ref value);
				}
			}
			else
			{
				Vector2 vector = mousePosCS - keyframePos;

				if (vector.X > 0.0001f)
				{
					value.outTangent = vector.Y / vector.X;
				}
				else
				{
					value.outTangent = float.PositiveInfinity;
				}

				CurveUtility.SetKeyTangentMode(ref value, 1, TangentMode.Editable);
				CalcTangentForButton(outTan, selectedControl, ref value);
				if (!CurveUtility.GetKeyBroken(value))
				{
					value.inTangent = value.outTangent;

					CurveUtility.SetKeyTangentMode(ref value, 0, TangentMode.Editable);
					CalcTangentForButton(inTan, selectedControl, ref value);
				}
			}
			curves[l].MoveKey(key, ref value);
			CurveUtility.UpdateTangentsFromModeSurrounding(curves[l], key);
			selectedControl.UserData = ((value.time, l, outTan, inTan));
			Squid.UI.isDirty = true;
		}

		private void CalcTangentForButton(DraggableButton button, Control center, ref Keyframe value)
		{
			var keyframePos = new Vector2(value.time, value.value);
			var condition = button.Name is "outTan";
			var pointInVS = RegionDrawer.CurveToViewSpace(keyframePos, scale, translation);
			var point = RegionDrawer.CurveToViewSpace(keyframePos.RotateAroundPivot(keyframePos + new Vector2(condition ? 1 : -1, 0), new Vector3((float)Math.Atan(condition ? value.outTangent : value.inTangent), 0, 0)), scale, translation);
			var dir = (point - pointInVS).Normalize() * 50;
			button.UserData = new Point((int)dir.X, (int)dir.Y);
			var pos = new Point(center.Position.x + 6, center.Position.y + 6) + (Point)button.UserData;
			ChangePositionWithoutEvent(button, new Point(pos.x - 6, pos.y - 6));
		}

		private void DraggableButton_MouseDown(Control sender, MouseEventArgs args)
		{
			var (time, l, sentOutTan, sentInTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)sender.UserData;
			var key = FindKeyframe(time, l);
			ref var curves = ref drawer.Value;
			var value = curves[l].keys[key];

			if (args.Button is 1)
			{
				CurveMenuManager.selected = new (int, int, Keyframe)[] { (l, key, value) };
				//CurveMenuManager.selectedKeyfr = clickedKeyframe;
				CurveMenuManager.updateSelected = UpdateClickedKeyfr;
				editMenu.Frame.Controls.Clear();

				editMenu.AddItem("Edit key");//edit with keyboard
				editMenu.AddItem("Edit tangents");
				var delButton = editMenu.AddItem("Delete Key");
				delButton.Name = "delete";
				delButton.MouseClick += DelButton_MouseClick;
				delButton.Text = "Delete Key" /*+ (clickedKeyframe[0].Count + clickedKeyframe[1].Count > 1 ? "s" : "")*/;
				delButton.UserData = new Control[] { sender };

				CurveMenuManager.AddTangentMenuItems(editMenu, curves);

				editMenu.Position = new Point(sender.Position.x + 6, sender.Position.y + 6);
				editMenu.IsVisible = true;
				editMenu.Open();
			}
			if (selectedControl != null)
			{
				var (_, _, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)selectedControl.UserData;
				inTan.IsVisible = false;
				outTan.IsVisible = false;
			}
			sentOutTan.IsVisible = IsOutTanVisible(l, key);
			sentInTan.IsVisible = IsInTanVisible(l, key);
			selectedControl = sender;
		}

		private bool IsOutTanVisible(int curveId, int key)
		{
			ref var curves = ref drawer.Value;
			var tanMode = CurveUtility.GetKeyTangentMode(ref curves[curveId].keys[key], 1);
			return key < curves[curveId].keys.Length - 1 && (tanMode is TangentMode.Editable or TangentMode.Smooth);
		}

		private bool IsInTanVisible(int curveId, int key)
		{
			ref var curves = ref drawer.Value;
			var tanMode = CurveUtility.GetKeyTangentMode(ref curves[curveId].keys[key], 0);
			return key > 0 && (tanMode is TangentMode.Editable or TangentMode.Smooth);
		}

		private int FindKeyframe(float time, int curveId)
		{
			ref var curves = ref drawer.Value;
			return Array.FindIndex(curves[curveId].keys, keyframe => time == keyframe.time);
		}

		private void CurvesView_SizeChanged(Control sender)
		{
			var size = Window.windows[attachedToWindow].Size;
			mainArea = (0, 29, size.width, size.height);
			swatchArea = (40, 40 + Location.y, size.width - 80, size.height - 80 - Location.y);
			var max = drawer.curvesRange.height + (drawer.curvesRange.y < 0 ? drawer.curvesRange.y : 0);
			var maxY = drawer.curvesRange.width + (drawer.curvesRange.x < 0 ? drawer.curvesRange.x : 0);
			scale = new Vector2(swatchArea.width / (drawer.curvesRange.width), -swatchArea.height / (drawer.curvesRange.height));//max-drawer.curvesRange.y
			translation = new Vector2(-drawer.curvesRange.x * scale.X + swatchArea.x, swatchArea.height - drawer.curvesRange.y * scale.Y + swatchArea.y);
			mainView.camera.SetOrthoMatrix(size.width, size.height);
			ref var curves = ref drawer.Value;
			foreach (var child in Childs)
			{
				if (child.Name != "inTan" && child.Name != "outTan" && child is DraggableButton button)
				{
					var (time, l, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)child.UserData;
					var key = FindKeyframe(time, l);
					var value = curves[l].keys[key];
					var point = RegionDrawer.CurveToViewSpace(new Vector2(value.time, value.value), scale, translation);
					ChangePositionWithoutEvent(button, new Point((int)point.X - 10, (int)point.Y - Location.y - 6));
					var outPos = new Point(button.Position.x + 6, button.Position.y + 6) + (Point)outTan.UserData;
					ChangePositionWithoutEvent(outTan, new Point(outPos.x - 6, outPos.y - 6));
					var inPos = new Point(button.Position.x + 6, button.Position.y + 6) + (Point)inTan.UserData;
					ChangePositionWithoutEvent(inTan, new Point(inPos.x - 6, inPos.y - 6));
				}
			}
		}

		private void ChangePositionWithoutEvent(Control control, Point position)
		{
			control.NoEvents = true;
			control.Position = position;
			control.NoEvents = false;
		}

		private void DraggableButton_PositionChanged(Control sender)
		{
			var (time, l, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)sender.UserData;
			var key = FindKeyframe(time, l);
			ref var curves = ref drawer.Value;
			var value = curves[l].keys[key];
			var newPos = draggingCurveId is -1 ? RegionDrawer.ViewToCurveSpace(new Vector2(sender.Position.x + 10, sender.Position.y + Location.y + 6), scale, translation) :
					 new Vector2(value.time, RegionDrawer.ViewToCurveSpace(new Vector2(sender.Position.x + 10, sender.Position.y + Location.y + 6), scale, translation).Y);

			//CheckIfOutsideArea(ref newPos);
			var newKeyFr = new Keyframe(newPos.X, newPos.Y, value.inTangent, value.outTangent);
			newKeyFr.tangentMode = value.tangentMode;

			if (key < 0)
			{
				showDrag = curves[l].AddKey(ref newKeyFr);
				if (showDrag < 0)
				{
					showDrag = 0;
				}
			}
			else
				showDrag = curves[l].MoveKey(key, ref newKeyFr);

			while (showDrag == -1)
			{
				showDrag = Squid.UI.MouseDelta.x < 0 ? key - 1 : key;
				curves[l].RemoveKey(showDrag);
				showDrag = curves[l].AddKey(ref newKeyFr);
			}

			if (showDrag != -1)
			{
				//Console.WriteLine("update");
				//CurveUtility.UpdateTangentsFromMode(drawer.Value[l], showDrag);
				CurveUtility.UpdateTangentsFromModeSurrounding(curves[l], showDrag);
			}
			if (draggingCurveId != -1)
			{
				var point = RegionDrawer.CurveToViewSpace(new Vector2(newKeyFr.time, newKeyFr.value), scale, translation);
				ChangePositionWithoutEvent(sender, new Point((int)point.X - 10, (int)point.Y - Location.y - 6));
			}
			else
			{
				outTan.IsVisible = IsOutTanVisible(l, showDrag);
				inTan.IsVisible = IsInTanVisible(l, showDrag);
				badge.Position = new Point((int)(sender.Position.x + 5f), (int)(sender.Position.y + 15f));
				badge.Text = $"{value.time:F3}, {value.value:F3}";
				badge.IsVisible = true;
			}
			if (outTan.IsVisible) CalcTangentForButton(outTan, sender, ref value);
			if (inTan.IsVisible) CalcTangentForButton(inTan, sender, ref value);
			sender.UserData = ((curves[l].keys[showDrag].time, l, outTan, inTan));
			Squid.UI.isDirty = true;
		}

		private void UI_MouseUp(Control sender, MouseEventArgs args)
		{
			showDrag = -1;
			badge.IsVisible = false;
			foreach (var child in Childs)
			{
				if (child.Name != "inTan" && child.Name != "outTan" && child is DraggableButton button)
				{
					//var (value, l, outTan, inTan) = (ValueTuple<Keyframe, int, DraggableButton, DraggableButton>)child.UserData;
					button.StopDrag();
				}
			}
			draggingCurveId = -1;
			var obj = Squid.UI.currentCanvas.GetControlAt(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
			Desktop.CurrentCursor = obj is null ? CursorNames.Default : obj.Cursor;
		}

		private void UI_MouseMove(Control sender, MouseEventArgs args)
		{
			if (!Window.windows.ContainsKey(attachedToWindow)) return;
			var mousePos = new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
			if (tracking > -1)
			{
				var x = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation).X;
				ref var curve = ref drawer.Value[tracking];
				var keyInCV = new Vector2(x, curve.Evaluate(x));
				CheckIfOutsideArea(ref keyInCV);
				var keyInVS = RegionDrawer.CurveToViewSpace(keyInCV, scale, translation);

				badge.Position = new Point((int)keyInVS.X, (int)keyInVS.Y - 15);
				badge.Text = $"{x:F3}, {curve.Evaluate(x):F3}";
				// badge.SizeToContents();
				badge.IsVisible = true;
				return;
			}
			//Squid.UI.isDirty = true;
			//editAxis = -1;
		}

		private void CurvesView_KeyDown(Control sender, KeyEventArgs args)
		{
			if (args.Key == Keys.ESCAPE)
			{
				tracking = -1;
				badge.IsVisible = false;
			}
			//Unfocus();
		}

		private void TrackButton_MouseClick(Control sender, MouseEventArgs args)
		{
			tracking = (int)sender.UserData;
			badge.IsVisible = true;
			Focus();
		}

		private void AddButton_MouseClick(Control sender, MouseEventArgs args)
		{
			object[] data = sender.UserData as object[];
			int curveId = (int)data[0];
			float time = (float)data[1];
			var startAngle = EvaluateCurveDeltaSlow(time, curveId);
			ref var curve = ref drawer.Value[curveId];
			var newPoint = new Vector2(time, curve.Evaluate(time));
			CheckIfOutsideArea(ref newPoint);
			var newkeyfr = new Keyframe(newPoint.X, newPoint.Y, startAngle, startAngle);
			var keyId = curve.AddKey(ref newkeyfr);
			Color c = new Color(drawer.curveColor.R, drawer.curveColor.G, drawer.curveColor.B, (byte)(drawer.curveColor.A * 0.75f));
			c = PrepareColorForCurve(c, curveId);
			AddButtonsForNewKey(newkeyfr, curveId, ref c);
			//CurvesView_SizeChanged(this);

			Squid.UI.isDirty = true;
		}

		private void DelButton_MouseClick(Control sender, MouseEventArgs args)
		{
			var data = sender.UserData as Control[];
			ref var curves = ref drawer.Value;
			foreach (var toDelete in data)
			{
				var (time, l, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)toDelete.UserData;
				var key = FindKeyframe(time, l);
				var value = curves[l].keys[key];
				curves[l].RemoveKey(Array.IndexOf(curves[l].keys, value));
				Childs.Remove(toDelete);
				Childs.Remove(inTan);
				Childs.Remove(outTan);
				if (toDelete == selectedControl)
					selectedControl = null;

			}

			Squid.UI.isDirty = true;
		}

		private void Panel_MouseUp(Control sender, MouseEventArgs args)
		{
			var mousePos = new Vector2(Squid.UI.MousePosition.x, Squid.UI.MousePosition.y);
			var pos = RegionDrawer.ViewToCurveSpace(mousePos, scale, translation);
			var rightClick = args.Button is 1;
			var checkMousePos = new bool[drawer.Value.Length];
			ref var curves = ref drawer.Value;
			foreach (var (id, curve) in curves.Indexed())
			{
				checkMousePos[id] = Math.Abs(curve.Evaluate(pos.X) - pos.Y) < 1f;
			}

			if (rightClick)
			{
				foreach (var (id, condition) in checkMousePos.Indexed())
				{
					if (!condition) continue;

					var addButton = menu.Dropdown.GetControl("add");
					addButton.UserData = new object[]
					 {
				 id,
				 pos.X,
					 };
					var trackButton = menu.Dropdown.GetControl("track");
					trackButton.UserData = id;

					menu.Position = new Point((int)mousePos.X - Location.x, (int)mousePos.Y - Location.y);
					menu.IsVisible = true;
					menu.Open();
				}
				return;
			}
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
			var size = Window.windows[attachedToWindow].Size;

			var max = drawer.curvesRange.height + (drawer.curvesRange.y < 0 ? drawer.curvesRange.y : 0);
			var maxY = drawer.curvesRange.width + (drawer.curvesRange.x < 0 ? drawer.curvesRange.x : 0);
			//MainWindow.backendRenderer.WriteDepth(false);
			ref var curve = ref Pipeline.Get<Mesh>().GetAsset("dynamic_curve");
			Color c;
			RegionDrawer.line2dMat.BindProperty("width", 2f);
			for (int j = 0; j < drawer.lines.Length; j++)
			{
				c = new Color(drawer.curveColor.R, drawer.curveColor.G, drawer.curveColor.B, (byte)(drawer.curveColor.A * 0.75f));
				c = PrepareColorForCurve(c, j);
				RegionDrawer.line2dMat.BindProperty("color", c);
				Line2dVertexFormat[] verts = new Line2dVertexFormat[(drawer.lines[j].Length - 1) * 4];
				var inds = new ushort[(drawer.lines[j].Length - 1) * 6];

				for (int i = 0; i < drawer.lines[j].Length - 1; i++)
				{
					var start = RegionDrawer.CurveToViewSpace(drawer.lines[j][i], scale, translation);
					var end = RegionDrawer.CurveToViewSpace(drawer.lines[j][i + 1], scale, translation);
					RegionDrawer.GenerateCurveMesh(verts.AsSpan()[(i * 4)..], inds.AsSpan()[(i * 6)..], i * 4, start, end);
				}
				curve.LoadIndices<ushort>(inds);
				curve.LoadVertices<Line2dVertexFormat>(verts);
				RegionDrawer.line2dMat.Draw();
			}
			c = PrepareColorForRegion(drawer.curveColor);
			RegionDrawer.polyfill2dMat.BindProperty("color", c);

			ref var polyfill = ref Pipeline.Get<Mesh>().GetAsset("dynamic_polyfill");
			var array = new Basic2dVertexFormat[drawer.region.Length];
			var indices = new ushort[drawer.region.Length];
			for (int i = 0; i < drawer.region.Length; i++)
			{
				indices[i] = (ushort)i;
				array[i].vertex_position = RegionDrawer.CurveToViewSpace(drawer.region[i], scale, translation);
			}
			polyfill.LoadVertices<Basic2dVertexFormat>(array);
			polyfill.LoadIndices<ushort>(indices);


			GridGUI();
			RegionDrawer.polyfill2dMat.Draw();
			//MainWindow.backendRenderer.WriteDepth();
			if (selectedControl != null)
			{
				var col = new Color(190, 200, 200, 200);
				if (false/*base.settings.useFocusColors && !hasFocus*/)
				{
					col = new Color((int)(col.A * 0.5f), (int)(col.R * 0.5f), (int)(col.G * 0.5f), (int)(col.B * 0.5f));
				}
				var color2 = Color.Lerp(drawer.curveColor, Color.White, 0.2f);
				var (_, _, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)selectedControl.UserData;
				RegionDrawer.line2dMat.BindProperty("color", col);
				if (outTan.IsVisible)
				{
					//MainEditorView.editorBackendRenderer.DrawLine(selectedControl.Location.x + 6, selectedControl.Location.y + 6, 0, outTan.Location.x + 6, outTan.Location.y + 6, 0, ref col.r);
					Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
					var inds = new ushort[6];
					RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, new Vector2(selectedControl.Location.x + 6, selectedControl.Location.y + 6), new Vector2(outTan.Location.x + 6, outTan.Location.y + 6));

					curve.LoadIndices<ushort>(inds);
					curve.LoadVertices<Line2dVertexFormat>(verts);
					RegionDrawer.line2dMat.Draw();
				}
				if (inTan.IsVisible)
				{
					//MainEditorView.editorBackendRenderer.DrawLine(selectedControl.Location.x + 6, selectedControl.Location.y + 6, 0, inTan.Location.x + 6, inTan.Location.y + 6, 0, ref col.r);
					Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
					var inds = new ushort[6];
					RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, new Vector2(selectedControl.Location.x + 6, selectedControl.Location.y + 6), new Vector2(inTan.Location.x + 6, inTan.Location.y + 6));

					curve.LoadIndices<ushort>(inds);
					curve.LoadVertices<Line2dVertexFormat>(verts);
					RegionDrawer.line2dMat.Draw();
				}
			}
		}
		private void UpdateClickedKeyfr((int, int, Keyframe)[] newSelected)
		{
			var (_, _, outTan, inTan) = (ValueTuple<float, int, DraggableButton, DraggableButton>)selectedControl.UserData;
			foreach (var (curveId, id, keyframe) in newSelected)
			{
				var tmpKeyframe = keyframe;
				CalcTangentForButton(outTan, selectedControl, ref tmpKeyframe);
				CalcTangentForButton(inTan, selectedControl, ref tmpKeyframe);
				selectedControl.UserData = ((keyframe.time, curveId, outTan, inTan));
				outTan.IsVisible = IsOutTanVisible(curveId, id);
				inTan.IsVisible = IsInTanVisible(curveId, id);
			}
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
			if (curveId == draggingCurveId)
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
			if (draggingCurveId < -1)
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
		}

		protected override void DrawBefore()
		{
			// MainWindow.backendRenderer.ChangeShader();
			OnGUI();
		}

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
			ref var curve = ref Pipeline.Get<Mesh>().GetAsset("dynamic_curve");
			RegionDrawer.line2dMat.BindProperty("width", 1f);
			for (int i = 0; i < hTicks.tickLevels; i++)
			{
				float strengthOfLevel = hTicks.GetStrengthOfLevel(i);
				if (strengthOfLevel > 0)
				{
					c = curveSettings.hTickStyle.color * new Color(1f, 1f, 1f, strengthOfLevel) * new Color(1f, 1f, 1f, 0.75f);
					RegionDrawer.line2dMat.BindProperty("color", c);
					float[] ticksAtLevel = hTicks.GetTicksAtLevel(i, true);
					for (int j = 0; j < ticksAtLevel.Length; j++)
					{
						ticksAtLevel[j] /= axisUiScalars.X;
						if (ticksAtLevel[j] > drawer.curvesRange.x && ticksAtLevel[j] < maxX)
						{
							Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
							var inds = new ushort[6];
							RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(ticksAtLevel[j], num), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(ticksAtLevel[j], num2), scale, translation));

							curve.LoadIndices<ushort>(inds);
							curve.LoadVertices<Line2dVertexFormat>(verts);
							RegionDrawer.line2dMat.Draw();
						}
					}
				}
			}
			c = curveSettings.hTickStyle.color * new Color(1f, 1f, 1f, 1f) * new Color(1, 1, 1, 0.75f);
			RegionDrawer.line2dMat.BindProperty("color", c);
			if (drawer.curvesRange.x != -float.PositiveInfinity)
			{
				Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
				var inds = new ushort[6];
				RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(drawer.curvesRange.x, num), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(drawer.curvesRange.x, num2), scale, translation));

				curve.LoadIndices<ushort>(inds);
				curve.LoadVertices<Line2dVertexFormat>(verts);
				RegionDrawer.line2dMat.Draw();
			}
			if (maxX != float.PositiveInfinity)
			{
				Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
				var inds = new ushort[6];
				RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(maxX, num), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(maxX, num2), scale, translation));

				curve.LoadIndices<ushort>(inds);
				curve.LoadVertices<Line2dVertexFormat>(verts);
				RegionDrawer.line2dMat.Draw();
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
					RegionDrawer.line2dMat.BindProperty("color", c);
					float[] ticksAtLevel2 = vTicks.GetTicksAtLevel(k, true);
					for (int l = 0; l < ticksAtLevel2.Length; l++)
					{
						ticksAtLevel2[l] /= axisUiScalars.Y;
						if (ticksAtLevel2[l] > drawer.curvesRange.y && ticksAtLevel2[l] < maxY)
						{
							Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
							var inds = new ushort[6];
							RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(num, ticksAtLevel2[l]), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(num2, ticksAtLevel2[l]), scale, translation));

							curve.LoadIndices<ushort>(inds);
							curve.LoadVertices<Line2dVertexFormat>(verts);
							RegionDrawer.line2dMat.Draw();
						}
					}
				}
			}
			c = curveSettings.vTickStyle.color * new Color(1f, 1f, 1f, 1f) * new Color(1, 1, 1, 0.75f);
			RegionDrawer.line2dMat.BindProperty("color", c);
			if (drawer.curvesRange.y != -float.PositiveInfinity)
			{
				Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
				var inds = new ushort[6];
				RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(num, drawer.curvesRange.y), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(num2, drawer.curvesRange.y), scale, translation));

				curve.LoadIndices<ushort>(inds);
				curve.LoadVertices<Line2dVertexFormat>(verts);
				RegionDrawer.line2dMat.Draw();
			}
			if (maxY != float.PositiveInfinity)
			{
				Line2dVertexFormat[] verts = new Line2dVertexFormat[4];
				var inds = new ushort[6];
				RegionDrawer.GenerateCurveMesh(verts.AsSpan(), inds.AsSpan(), 0, RegionDrawer.CurveToViewSpace(new Vector2(num, maxY), scale, translation), RegionDrawer.CurveToViewSpace(new Vector2(num2, maxY), scale, translation));

				curve.LoadIndices<ushort>(inds);
				curve.LoadVertices<Line2dVertexFormat>(verts);
				RegionDrawer.line2dMat.Draw();
			}

			if (curveSettings.showAxisLabels)
			{
				if (curveSettings.hTickStyle.distLabel > 0 && axisUiScalars.X > 0)
				{
					int levelWithMinSeparation = hTicks.GetLevelWithMinSeparation((float)curveSettings.hTickStyle.distLabel);
					int numberOfDecimalsForMinimumDifference = NumericsExtensions.Clamp(-(int)Math.Floor(Math.Log10(hTicks.GetPeriodOfLevel(levelWithMinSeparation))), 0, 15);
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

					int numberOfDecimalsForMinimumDifference2 = NumericsExtensions.Clamp(-(int)Math.Floor(Math.Log10(hTicks.GetPeriodOfLevel(levelWithMinSeparation2))), 0, 15); ;
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
		}

		public float EvaluateCurveDeltaSlow(float time, int curveId)
		{
			float num = 0.0001f;
			ref var curve = ref drawer.Value[curveId];
			return (curve.Evaluate(time + num) - curve.Evaluate(time - num)) / (num * 2f);
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
					//Curve curveWrapper = drawer.Value[i].ToObject<Curve>();
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