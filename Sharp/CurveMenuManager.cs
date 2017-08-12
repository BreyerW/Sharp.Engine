using System;
using System.Collections.Generic;
using OpenTK;
using Squid;
using System.Linq;

namespace Sharp
{
    public static class CurveMenuManager
    {
        //public static Dictionary<Vector2, Keyframe>[] selectedKeyfr;
        public static Action<(int, int, Keyframe)[]> updateSelected;

        public static (int, int, Keyframe)[] selected;

        //
        // Methods
        //
        public static void AddTangentMenuItems(Menu menu, Curve[] keyList)
        {
            bool flag = keyList.Length > 0;
            bool on = flag;
            bool on2 = flag;
            bool on3 = flag;
            bool on4 = flag;
            bool flag2 = flag;
            bool flag3 = flag;
            bool flag4 = flag;
            bool flag5 = flag;
            bool flag6 = flag;
            bool flag7 = flag;
            foreach (var (curveId, index, current) in selected)
            {
                if (!keyList[curveId].keys.Contains(current))
                    continue;
                Keyframe keyframe = current;
                TangentMode keyTangentMode = CurveUtility.GetKeyTangentMode(ref keyframe, 0);
                TangentMode keyTangentMode2 = CurveUtility.GetKeyTangentMode(ref keyframe, 1);
                bool keyBroken = CurveUtility.GetKeyBroken(keyframe);
                if (keyTangentMode != TangentMode.Smooth || keyTangentMode2 != TangentMode.Smooth)
                {
                    on = false;
                }
                if (keyBroken || keyTangentMode != TangentMode.Editable || keyTangentMode2 != TangentMode.Editable)
                {
                    on2 = false;
                }
                if (keyBroken || keyTangentMode != TangentMode.Editable || keyframe.inTangent != 0 || keyTangentMode2 != TangentMode.Editable || keyframe.outTangent != 0)
                {
                    on3 = false;
                }
                if (!keyBroken)
                {
                    on4 = false;
                }
                if (!keyBroken || keyTangentMode != TangentMode.Editable)
                {
                    flag2 = false;
                }
                if (!keyBroken || keyTangentMode != TangentMode.Linear)
                {
                    flag3 = false;
                }
                if (!keyBroken || keyTangentMode != TangentMode.Stepped)
                {
                    flag4 = false;
                }
                if (!keyBroken || keyTangentMode2 != TangentMode.Editable)
                {
                    flag5 = false;
                }
                if (!keyBroken || keyTangentMode2 != TangentMode.Linear)
                {
                    flag6 = false;
                }
                if (!keyBroken || keyTangentMode2 != TangentMode.Stepped)
                {
                    flag7 = false;
                }
            }
            if (flag)
            {
                var auto = menu.AddItem("Auto");// on
                auto.MouseClick += SetSmooth;
                auto.UserData = keyList;

                var free = menu.AddItem("FreeSmooth");//, on2
                free.MouseClick += SetEditable;
                free.UserData = keyList;

                var flat = menu.AddItem("Flat");//, on3
                flat.UserData = keyList;
                flat.MouseClick += SetFlat;

                var broken = menu.AddItem("Broken");//on4
                broken.UserData = keyList;
                broken.MouseClick += SetBroken;
                //menu.AddSeparator(string.Empty);

                var leftFree = menu.AddItem("Left Tangent/Free");//flag2
                leftFree.UserData = keyList;
                leftFree.MouseClick += SetLeftEditable;

                var leftLinear = menu.AddItem("Left Tangent/Linear");//flag3,
                leftLinear.UserData = keyList;
                leftLinear.MouseClick += SetLeftLinear;

                var leftConstant = menu.AddItem("Left Tangent/Constant");//flag4,
                leftConstant.UserData = keyList;
                leftConstant.MouseClick += SetLeftConstant;

                var rightFree = menu.AddItem("Right Tangent/Free"); //flag5,
                rightFree.UserData = keyList;
                rightFree.MouseClick += SetRightEditable;

                var rightLinear = menu.AddItem("Right Tangent/Linear");//flag6,
                rightLinear.UserData = keyList;
                rightLinear.MouseClick += SetRightLinear;

                var rightConstant = menu.AddItem("Right Tangent/Constant"); //flag7,
                rightConstant.UserData = keyList;
                rightConstant.MouseClick += SetRightConstant;

                var bothFree = menu.AddItem("Both Tangents/Free");//flag5 && flag2,
                bothFree.UserData = keyList;
                bothFree.MouseClick += SetBothEditable;

                var bothLinear = menu.AddItem("Both Tangents/Linear");//flag6 && flag3,
                bothLinear.UserData = keyList;
                bothLinear.MouseClick += SetBothLinear;

                var bothConstant = menu.AddItem("Both Tangents/Constant");//flag7 && flag4,
                bothConstant.UserData = keyList;
                bothConstant.MouseClick += SetBothConstant;
            }
            else
            {
                /*    menu.AddDisabledItem(new GUIContent("Auto"));
                    menu.AddDisabledItem(new GUIContent("FreeSmooth"));
                    menu.AddDisabledItem(new GUIContent("Flat"));
                    menu.AddDisabledItem(new GUIContent("Broken"));
                    menu.AddSeparator(string.Empty);
                    menu.AddDisabledItem(new GUIContent("Left Tangent/Free"));
                    menu.AddDisabledItem(new GUIContent("Left Tangent/Linear"));
                    menu.AddDisabledItem(new GUIContent("Left Tangent/Constant"));
                    menu.AddDisabledItem(new GUIContent("Right Tangent/Free"));
                    menu.AddDisabledItem(new GUIContent("Right Tangent/Linear"));
                    menu.AddDisabledItem(new GUIContent("Righ tTangent/Constant"));
                    menu.AddDisabledItem(new GUIContent("Both Tangents/Free"));
                    menu.AddDisabledItem(new GUIContent("Both Tangents/Linear"));
                    menu.AddDisabledItem(new GUIContent("Both Tangents/Constant"));*/
            }
        }

        public static void Flatten(Curve[] keysToSet)
        {
            var newSelectedKeyfr = new List<(int, int, Keyframe)>();
            foreach (var (i, id, _) in selected)
            {
                Curve curve = keysToSet[i];
                if (!keysToSet[i].keys.Contains(curve.keys[id]))
                    continue;

                Keyframe keyframe = curve.keys[id];
                keyframe.inTangent = 0;
                keyframe.outTangent = 0;
                var newid = curve.MoveKey(id, ref keyframe);
                CurveUtility.UpdateTangentsFromModeSurrounding(curve, newid);
                newSelectedKeyfr.Add((i, newid, curve.keys[newid]));
            }
            updateSelected(newSelectedKeyfr.ToArray());
            UI.isDirty = true;
        }

        public static void SetBoth(TangentMode mode, Curve[] keysToSet)
        {
            var newSelectedKeyfr = new List<(int, int, Keyframe)>();

            foreach (var (i, id, current) in selected)
            {
                if (!keysToSet[i].keys.Contains(current))
                    continue;
                Curve curve = keysToSet[i];
                Keyframe keyframe = current;
                CurveUtility.SetKeyBroken(ref keyframe, false);
                CurveUtility.SetKeyTangentMode(ref keyframe, 1, mode);
                CurveUtility.SetKeyTangentMode(ref keyframe, 0, mode);
                if (mode == TangentMode.Editable)
                {
                    float num = CurveUtility.CalculateSmoothTangent(ref keyframe);
                    keyframe.inTangent = num;
                    keyframe.outTangent = num;
                }
                var newid = curve.MoveKey(id, ref keyframe);
                CurveUtility.UpdateTangentsFromModeSurrounding(curve, newid);
                newSelectedKeyfr.Add((i, newid, curve.keys[newid]));
            }
            updateSelected(newSelectedKeyfr.ToArray());
            UI.isDirty = true;
        }

        public static void SetBothConstant(Control sender, MouseEventArgs args)
        {
            SetTangent(2, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetBothEditable(Control sender, MouseEventArgs args)
        {
            SetTangent(2, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetBothLinear(Control sender, MouseEventArgs args)
        {
            SetTangent(2, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetBroken(Control sender, MouseEventArgs args)
        {
            var newSelectedKeyfr = new List<(int, int, Keyframe)>();

            Curve[] list = (Curve[])sender.UserData; ;

            foreach (var (i, id, current) in selected)
            {
                if (!list[i].keys.Contains(current))
                    continue;
                Curve curve = list[i];
                Keyframe keyframe = current;
                CurveUtility.SetKeyBroken(ref keyframe, true);
                if (CurveUtility.GetKeyTangentMode(ref keyframe, 1) == TangentMode.Smooth)
                {
                    CurveUtility.SetKeyTangentMode(ref keyframe, 1, TangentMode.Editable);
                }
                if (CurveUtility.GetKeyTangentMode(ref keyframe, 0) == TangentMode.Smooth)
                {
                    CurveUtility.SetKeyTangentMode(ref keyframe, 0, TangentMode.Editable);
                }
                var newid = curve.MoveKey(id, ref keyframe);
                CurveUtility.UpdateTangentsFromModeSurrounding(curve, newid);
                newSelectedKeyfr.Add((i, newid, curve.keys[newid]));
            }
            updateSelected(newSelectedKeyfr.ToArray());
        }

        public static void SetEditable(Control sender, MouseEventArgs args)
        {
            SetBoth(TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetFlat(Control sender, MouseEventArgs args)
        {
            SetBoth(TangentMode.Editable, (Curve[])sender.UserData);
            Flatten((Curve[])sender.UserData);
        }

        public static void SetLeftConstant(Control sender, MouseEventArgs args)
        {
            SetTangent(0, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetLeftEditable(Control sender, MouseEventArgs args)
        {
            SetTangent(0, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetLeftLinear(Control sender, MouseEventArgs args)
        {
            SetTangent(0, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetRightConstant(Control sender, MouseEventArgs args)
        {
            SetTangent(1, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetRightEditable(Control sender, MouseEventArgs args)
        {
            SetTangent(1, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetRightLinear(Control sender, MouseEventArgs args)
        {
            SetTangent(1, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetSmooth(Control sender, MouseEventArgs args)
        {
            SetBoth(TangentMode.Smooth, (Curve[])sender.UserData);
        }

        public static void SetTangent(int leftRight, TangentMode mode, Curve[] keysToSet)
        {
            var newSelectedKeyfr = new List<(int, int, Keyframe)>();

            foreach (var (i, id, current) in selected)
            {
                if (!keysToSet[i].keys.Contains(current))
                    continue;
                Curve curve = keysToSet[i];
                Keyframe keyframe = current;
                CurveUtility.SetKeyBroken(ref keyframe, true);
                if (leftRight == 2)
                {
                    CurveUtility.SetKeyTangentMode(ref keyframe, 0, mode);
                    CurveUtility.SetKeyTangentMode(ref keyframe, 1, mode);
                }
                else
                {
                    CurveUtility.SetKeyTangentMode(ref keyframe, leftRight, mode);
                    if (CurveUtility.GetKeyTangentMode(ref keyframe, 1 - leftRight) == TangentMode.Smooth)
                    {
                        CurveUtility.SetKeyTangentMode(ref keyframe, 1 - leftRight, TangentMode.Editable);
                    }
                }
                if (mode == TangentMode.Stepped && (leftRight == 0 || leftRight == 2))
                {
                    keyframe.inTangent = float.PositiveInfinity;
                }
                if (mode == TangentMode.Stepped && (leftRight == 1 || leftRight == 2))
                {
                    keyframe.outTangent = float.PositiveInfinity;
                }
                var newid = curve.MoveKey(id, ref keyframe);
                CurveUtility.UpdateTangentsFromModeSurrounding(curve, newid);
                newSelectedKeyfr.Add((i, newid, curve.keys[newid]));
            }
            updateSelected(newSelectedKeyfr.ToArray());
            UI.isDirty = true;
        }
    }
}