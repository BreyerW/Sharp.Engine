using System;
using System.Collections.Generic;
using OpenTK;

namespace Sharp
{
    public enum TangentMode
    {
        Editable = 0,
        Smooth = 1,
        Linear = 2,
        Stepped = Linear | Smooth,
    }

    public enum TangentDirection
    {
        None,
        Left,
        Right
    }

    public static class CurveUtility
    {
        //
        // Static Fields
        //
        private const int kBrokenMask = 1;

        private const int kLeftTangentMask = 6;

        private const int kRightTangentMask = 24;

        //
        // Static Methods
        //
        private static float CalculateLinearTangent(Curve curve, int index, int toIndex)
        {
            return (curve.keys[index].value - curve.keys[toIndex].value) / (curve.keys[index].time - curve.keys[toIndex].time);
        }

        public static Vector2 RotateAroundPivot(this Vector2 pivot, Vector2 point, Vector3 angles)
        {
            var dir = new Vector3(point - pivot);
            dir = Quaternion.FromEulerAngles(angles) * dir;
            return dir.Xy + pivot;
        }

        public static Vector3 RotateAroundPivot(this Vector3 pivot, Vector3 point, Vector3 angles)
        {
            var dir = point - pivot;
            dir = Quaternion.FromEulerAngles(angles) * dir;
            return dir + pivot;
        }

        public static float CalculateSmoothTangent(Keyframe key)
        {
            if (key.inTangent == float.PositiveInfinity)
            {
                key.inTangent = 0;
            }
            if (key.outTangent == float.PositiveInfinity)
            {
                key.outTangent = 0;
            }
            return (key.outTangent + key.inTangent) * 0.5f;
        }

        public static bool GetKeyBroken(Keyframe key)
        {
            return (key.tangentMode & 1) != 0;
        }

        public static TangentMode GetKeyTangentMode(ref Keyframe key, int leftRight)
        {
            if (leftRight == 0)
            {
                return (TangentMode)((key.tangentMode & 6) >> 1);
            }
            return (TangentMode)((key.tangentMode & 24) >> 3);
        }

        public static void SetKeyBroken(ref Keyframe key, bool broken)
        {
            if (broken)
            {
                key.tangentMode |= 1;
            }
            else
            {
                key.tangentMode &= -2;
            }
        }

        public static void SetKeyModeFromContext(Curve curve, int keyIndex)
        {
            Keyframe key = curve.keys[keyIndex];
            bool flag = false;
            if (keyIndex > 0 && CurveUtility.GetKeyBroken(curve.keys[keyIndex - 1]))
            {
                flag = true;
            }
            if (keyIndex < curve.keys.Length - 1 && CurveUtility.GetKeyBroken(curve.keys[keyIndex + 1]))
            {
                flag = true;
            }
            CurveUtility.SetKeyBroken(ref key, flag);
            if (flag)
            {
                if (keyIndex > 0)
                {
                    CurveUtility.SetKeyTangentMode(ref key, 0, CurveUtility.GetKeyTangentMode(ref curve.keys[keyIndex - 1], 1));
                }
                if (keyIndex < curve.keys.Length - 1)
                {
                    CurveUtility.SetKeyTangentMode(ref key, 1, CurveUtility.GetKeyTangentMode(ref curve.keys[keyIndex + 1], 0));
                }
            }
            else
            {
                TangentMode mode = TangentMode.Smooth;
                if (keyIndex > 0 && CurveUtility.GetKeyTangentMode(ref curve.keys[keyIndex - 1], 1) != TangentMode.Smooth)
                {
                    mode = TangentMode.Editable;
                }
                if (keyIndex < curve.keys.Length - 1 && CurveUtility.GetKeyTangentMode(ref curve.keys[keyIndex + 1], 0) != TangentMode.Smooth)
                {
                    mode = TangentMode.Editable;
                }
                CurveUtility.SetKeyTangentMode(ref key, 0, mode);
                CurveUtility.SetKeyTangentMode(ref key, 1, mode);
            }
            curve.MoveKey(keyIndex, ref key);
        }

        public static void SetKeyTangentMode(ref Keyframe key, int leftRight, TangentMode mode)
        {
            if (leftRight == 0)
            {
                key.tangentMode &= -7;
                key.tangentMode |= (int)((int)mode << 1);
            }
            else
            {
                key.tangentMode &= -25;
                key.tangentMode |= (int)((int)mode << 3);
            }
            if (CurveUtility.GetKeyTangentMode(ref key, leftRight) != mode)
            {
                Console.WriteLine("bug");
            }
        }

        public static void UpdateTangentsFromMode(Curve curve, int index)
        {
            if (index < 0 || index >= curve.keys.Length)
            {
                return;
            }
            Keyframe key = curve.keys[index];
            if (CurveUtility.GetKeyTangentMode(ref key, 0) == TangentMode.Linear && index >= 1)
            {
                key.inTangent = CurveUtility.CalculateLinearTangent(curve, index, index - 1);
                curve.MoveKey(index, ref key);
            }
            else if (CurveUtility.GetKeyTangentMode(ref key, 0) == TangentMode.Stepped && index >= 1)
            {
                key.inTangent = float.PositiveInfinity;
                curve.MoveKey(index, ref key);
            }
            if (CurveUtility.GetKeyTangentMode(ref key, 1) == TangentMode.Linear && index + 1 < curve.keys.Length)
            {
                key.outTangent = CurveUtility.CalculateLinearTangent(curve, index, index + 1);
                curve.MoveKey(index, ref key);
            }
            else if (CurveUtility.GetKeyTangentMode(ref key, 1) == TangentMode.Stepped && index + 1 < curve.keys.Length)
            {
                key.outTangent = float.PositiveInfinity;
                curve.MoveKey(index, ref key);
            }
            if (CurveUtility.GetKeyTangentMode(ref key, 0) == TangentMode.Smooth || CurveUtility.GetKeyTangentMode(ref key, 1) == TangentMode.Smooth)
            {
                curve.SmoothTangents(index, 0);
            }
        }

        public static void UpdateTangentsFromMode(Curve curve)
        {
            for (int i = 0; i < curve.keys.Length; i++)
            {
                CurveUtility.UpdateTangentsFromMode(curve, i);
            }
        }

        public static void UpdateTangentsFromModeSurrounding(Curve curve, int index)
        {
            CurveUtility.UpdateTangentsFromMode(curve, index - 2);
            CurveUtility.UpdateTangentsFromMode(curve, index - 1);
            CurveUtility.UpdateTangentsFromMode(curve, index);
            CurveUtility.UpdateTangentsFromMode(curve, index + 1);
            CurveUtility.UpdateTangentsFromMode(curve, index + 2);
        }
    }

    /*  public class TickHandler
      {
          //
          // Fields
          //
          private float[] m_TickModulos = new float[0];

          private float[] m_TickStrengths = new float[0];

          private int m_SmallestTick;

          private int m_BiggestTick = -1;

          private float m_MinValue;

          private float m_MaxValue = 1;

          private float m_PixelRange = 1;

          //
          // Properties
          //
          public int tickLevels
          {
              get
              {
                  return this.m_BiggestTick - this.m_SmallestTick + 1;
              }
          }

          //
          // Methods
          //
          public int GetLevelWithMinSeparation(float pixelSeparation)
          {
              for (int i = 0; i < this.m_TickModulos.Length; i++)
              {
                  float num = this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue);
                  if (num >= pixelSeparation)
                  {
                      return i - this.m_SmallestTick;
                  }
              }
              return -1;
          }

          public float GetPeriodOfLevel(int level)
          {
              return this.m_TickModulos[Math.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1)];
          }

          public float GetStrengthOfLevel(int level)
          {
              return this.m_TickStrengths[this.m_SmallestTick + level];
          }

          public float[] GetTicksAtLevel(int level, bool excludeTicksFromHigherlevels)
          {
              int num = Math.Clamp(this.m_SmallestTick + level, 0, this.m_TickModulos.Length - 1);
              List<float> list = new List<float>();
              int num2 = Math.FloorToInt(this.m_MinValue / this.m_TickModulos[num]);
              int num3 = Math.CeilToInt(this.m_MaxValue / this.m_TickModulos[num]);
              for (int i = num2; i <= num3; i++)
              {
                  if (!excludeTicksFromHigherlevels || num >= this.m_BiggestTick || i % Math.RoundToInt(this.m_TickModulos[num + 1] / this.m_TickModulos[num]) != 0)
                  {
                      list.Add((float)i * this.m_TickModulos[num]);
                  }
              }
              return list.ToArray();
          }

          public void SetRanges(float minValue, float maxValue, float minPixel, float maxPixel)
          {
              this.m_MinValue = minValue;
              this.m_MaxValue = maxValue;
              this.m_PixelRange = maxPixel - minPixel;
          }

          public void SetTickModulos(float[] tickModulos)
          {
              this.m_TickModulos = tickModulos;
          }

          public void SetTickModulosForFrameRate(float frameRate)
          {
              if (frameRate != Math.Round(frameRate))
              {
                  this.SetTickModulos(new float[]
                  {
                       1 / frameRate,
                       5 / frameRate,
                       10 / frameRate,
                       50 / frameRate,
                       100 / frameRate,
                       500 / frameRate,
                       1000 / frameRate,
                       5000 / frameRate,
                       10000 / frameRate,
                       50000 / frameRate,
                       100000 / frameRate,
                       500000 / frameRate
                  });
              }
              else
              {
                  List<int> list = new List<int>();
                  int num = 1;
                  while ((float)num < frameRate)
                  {
                      if ((float)num == frameRate)
                      {
                          break;
                      }
                      int num2 = Math.RoundToInt(frameRate / (float)num);
                      if (num2 % 60 == 0)
                      {
                          num *= 2;
                          list.Add(num);
                      }
                      else
                      {
                          if (num2 % 30 == 0)
                          {
                              num *= 3;
                              list.Add(num);
                          }
                          else
                          {
                              if (num2 % 20 == 0)
                              {
                                  num *= 2;
                                  list.Add(num);
                              }
                              else
                              {
                                  if (num2 % 10 == 0)
                                  {
                                      num *= 2;
                                      list.Add(num);
                                  }
                                  else
                                  {
                                      if (num2 % 5 == 0)
                                      {
                                          num *= 5;
                                          list.Add(num);
                                      }
                                      else
                                      {
                                          if (num2 % 2 == 0)
                                          {
                                              num *= 2;
                                              list.Add(num);
                                          }
                                          else
                                          {
                                              if (num2 % 3 == 0)
                                              {
                                                  num *= 3;
                                                  list.Add(num);
                                              }
                                              else
                                              {
                                                  num = Mathf.RoundToInt(frameRate);
                                              }
                                          }
                                      }
                                  }
                              }
                          }
                      }
                  }
                  float[] array = new float[9 + list.Count];
                  for (int i = 0; i < list.Count; i++)
                  {
                      array[i] = 1 / (float)list[list.Count - i - 1];
                  }
                  array[array.Length - 1] = 3600;
                  array[array.Length - 2] = 1800;
                  array[array.Length - 3] = 600;
                  array[array.Length - 4] = 300;
                  array[array.Length - 5] = 60;
                  array[array.Length - 6] = 30;
                  array[array.Length - 7] = 10;
                  array[array.Length - 8] = 5;
                  array[array.Length - 9] = 1;
                  this.SetTickModulos(array);
              }
          }

          public void SetTickStrengths(float tickMinSpacing, float tickMaxSpacing, bool sqrt)
          {
              this.m_TickStrengths = new float[this.m_TickModulos.Length];
              this.m_SmallestTick = 0;
              this.m_BiggestTick = this.m_TickModulos.Length - 1;
              for (int i = this.m_TickModulos.Length - 1; i >= 0; i--)
              {
                  float num = this.m_TickModulos[i] * this.m_PixelRange / (this.m_MaxValue - this.m_MinValue);
                  this.m_TickStrengths[i] = (num - tickMinSpacing) / (tickMaxSpacing - tickMinSpacing);
                  if (this.m_TickStrengths[i] >= 1)
                  {
                      this.m_BiggestTick = i;
                  }
                  if (num <= tickMinSpacing)
                  {
                      this.m_SmallestTick = i;
                      break;
                  }
              }
              for (int j = this.m_SmallestTick; j <= this.m_BiggestTick; j++)
              {
                  this.m_TickStrengths[j] = Math.Clamp01(this.m_TickStrengths[j]);
                  if (sqrt)
                  {
                      this.m_TickStrengths[j] = Math.Sqrt(this.m_TickStrengths[j]);
                  }
              }
          }
      }*/
}