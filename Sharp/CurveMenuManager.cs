using System;
using System.Collections.Generic;
using OpenTK;
using Squid;

namespace Sharp
{
    public static class CurveMenuManager
    {
        public static Dictionary<Vector2, Keyframe>[] selectedKeyfr;
        public static Action<Dictionary<Vector2, Keyframe>[]> updateSelected;

        //
        // Methods
        //
        public static void AddTangentMenuItems(ListBox menu, Curve[] keyList)
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
            var l = 0;
            while (l < 2)
            {
                foreach (var current in keyList[l].keys)
                {
                    if (!selectedKeyfr[l].ContainsValue(current))
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
                l++;
            }
            if (flag)
            {
                /*  menu.Childs.Add("Auto", "", keyList).Clicked += SetSmooth;// on
                  menu.AddRow("FreeSmooth", "", keyList).Clicked += SetEditable;//, on2
                  menu.AddRow("Flat", "", keyList).Clicked += SetFlat;//, on3
                  menu.AddRow("Broken", "", keyList).Clicked += SetBroken;//on4
                  //menu.AddSeparator(string.Empty);
                  menu.AddRow("Left Tangent/Free", "", keyList).Clicked += SetLeftEditable;//flag2
                  menu.AddRow("Left Tangent/Linear", "", keyList).Clicked += SetLeftLinear;//flag3,
                  menu.AddRow("Left Tangent/Constant", "", keyList).Clicked += SetLeftConstant;//flag4,
                  menu.AddRow("Right Tangent/Free", "", keyList).Clicked += SetRightEditable; //flag5,
                  menu.AddRow("Right Tangent/Linear", "", keyList).Clicked += SetRightLinear;//flag6,
                  menu.AddRow("Right Tangent/Constant", "", keyList).Clicked += SetRightConstant; //flag7,
                  menu.AddRow("Both Tangents/Free", "", keyList).Clicked += SetBothEditable;//flag5 && flag2,
                  menu.AddRow("Both Tangents/Linear", "", keyList).Clicked += SetBothLinear;//flag6 && flag3,
                  menu.AddRow("Both Tangents/Constant", "", keyList).Clicked += SetBothConstant;//flag7 && flag4,
                  */
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
            var newSelectedKeyfr = new Dictionary<Vector2, Keyframe>[] { new Dictionary<Vector2, Keyframe>(), new Dictionary<Vector2, Keyframe>() };
            var l = 0;
            while (l < 2)
            {
                var id = -1;
                foreach (var current in keysToSet[l].keys)
                {
                    id++;
                    if (!selectedKeyfr[l].ContainsValue(current))
                        continue;
                    Curve curve = keysToSet[l];
                    Keyframe keyframe = current;
                    keyframe.inTangent = 0;
                    keyframe.outTangent = 0;
                    curve.MoveKey(id, ref keyframe);
                    CurveUtility.UpdateTangentsFromModeSurrounding(curve, id);
                    newSelectedKeyfr[l].Add(new Vector2(id, id), curve.keys[id]);
                }
                l++;
            }
            updateSelected(newSelectedKeyfr);
        }

        public static void SetBoth(TangentMode mode, Curve[] keysToSet)
        {
            var newSelectedKeyfr = new Dictionary<Vector2, Keyframe>[] { new Dictionary<Vector2, Keyframe>(), new Dictionary<Vector2, Keyframe>() };

            var l = 0;
            while (l < 2)
            {
                var id = -1;
                foreach (var current in keysToSet[l].keys)
                {
                    id++;
                    if (!selectedKeyfr[l].ContainsValue(current))
                        continue;
                    Curve curve = keysToSet[l];
                    Keyframe keyframe = current;
                    CurveUtility.SetKeyBroken(ref keyframe, false);
                    CurveUtility.SetKeyTangentMode(ref keyframe, 1, mode);
                    CurveUtility.SetKeyTangentMode(ref keyframe, 0, mode);
                    if (mode == TangentMode.Editable)
                    {
                        float num = CurveUtility.CalculateSmoothTangent(keyframe);
                        keyframe.inTangent = num;
                        keyframe.outTangent = num;
                    }
                    curve.MoveKey(id, ref keyframe);
                    CurveUtility.UpdateTangentsFromModeSurrounding(curve, id);
                    newSelectedKeyfr[l].Add(new Vector2(id, id), curve.keys[id]);
                }
                l++;
            }
            updateSelected(newSelectedKeyfr);
        }

        public static void SetBothConstant(Control sender, EventArgs args)
        {
            SetTangent(2, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetBothEditable(Control sender, EventArgs args)
        {
            SetTangent(2, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetBothLinear(Control sender, EventArgs args)
        {
            SetTangent(2, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetBroken(Control sender, EventArgs args)
        {
            var newSelectedKeyfr = new Dictionary<Vector2, Keyframe>[] { new Dictionary<Vector2, Keyframe>(), new Dictionary<Vector2, Keyframe>() };

            Curve[] list = (Curve[])sender.UserData;
            var l = 0;
            while (l < 2)
            {
                var id = -1;

                foreach (var current in list[l].keys)
                {
                    id++;
                    if (!selectedKeyfr[l].ContainsValue(current))
                        continue;
                    Curve curve = list[l];
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
                    curve.MoveKey(id, ref keyframe);
                    CurveUtility.UpdateTangentsFromModeSurrounding(curve, id);
                    newSelectedKeyfr[l].Add(new Vector2(id, id), curve.keys[id]);
                }
                l++;
            }
            updateSelected(newSelectedKeyfr);
        }

        public static void SetEditable(Control sender, EventArgs args)
        {
            SetBoth(TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetFlat(Control sender, EventArgs args)
        {
            SetBoth(TangentMode.Editable, (Curve[])sender.UserData);
            Flatten((Curve[])sender.UserData);
        }

        public static void SetLeftConstant(Control sender, EventArgs args)
        {
            SetTangent(0, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetLeftEditable(Control sender, EventArgs args)
        {
            SetTangent(0, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetLeftLinear(Control sender, EventArgs args)
        {
            SetTangent(0, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetRightConstant(Control sender, EventArgs args)
        {
            SetTangent(1, TangentMode.Stepped, (Curve[])sender.UserData);
        }

        public static void SetRightEditable(Control sender, EventArgs args)
        {
            SetTangent(1, TangentMode.Editable, (Curve[])sender.UserData);
        }

        public static void SetRightLinear(Control sender, EventArgs args)
        {
            SetTangent(1, TangentMode.Linear, (Curve[])sender.UserData);
        }

        public static void SetSmooth(Control sender, EventArgs args)
        {
            SetBoth(TangentMode.Smooth, (Curve[])sender.UserData);
        }

        public static void SetTangent(int leftRight, TangentMode mode, Curve[] keysToSet)
        {
            var newSelectedKeyfr = new Dictionary<Vector2, Keyframe>[] { new Dictionary<Vector2, Keyframe>(), new Dictionary<Vector2, Keyframe>() };

            var l = 0;
            while (l < 2)
            {
                var id = -1;
                foreach (var current in keysToSet[l].keys)
                {
                    id++;
                    if (!selectedKeyfr[l].ContainsValue(current))
                        continue;
                    Curve curve = keysToSet[l];
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
                    curve.MoveKey(id, ref keyframe);
                    CurveUtility.UpdateTangentsFromModeSurrounding(curve, id);
                    newSelectedKeyfr[l].Add(new Vector2(id, id), curve.keys[id]);
                }
                l++;
            }
            updateSelected(newSelectedKeyfr);
        }
    }
}