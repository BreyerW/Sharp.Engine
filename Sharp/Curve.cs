using System;
using System.Collections.Generic;
using OpenTK;
using System.Runtime.CompilerServices;
using System.Linq;

namespace Sharp
{
    public struct Curve
    {
        public Keyframe[] keys;
        public WrapMode preMode;
        public WrapMode postMode;

        public bool IsConstant
        {
            get => keys.Length <= 1;
        }

        public float Evaluate(float position)
        {
            //Array.Sort(keys, new KeyframeComparer());//move out of evaluate
            Keyframe first = keys[0];
            Keyframe last = keys[keys.Length - 1];

            if (position < first.time)
            {
                switch (preMode)
                {
                    case WrapMode.Constant:
                        //constant
                        return first.value;

                    case WrapMode.Cycle:
                        //start -> end / start -> end
                        int cycle = GetNumberOfCycle(position);
                        float virtualPos = position - (cycle * (last.time - first.time));
                        return GetCurvePosition(virtualPos);

                    case WrapMode.CycleOffset:
                        //make the curve continue (with no step) so must up the curve each cycle of delta(value)
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.time - first.time));
                        return (GetCurvePosition(virtualPos) + cycle * (last.value - first.value));

                    case WrapMode.Oscillate:
                        //go back on curve from end and target start
                        // start-> end / end -> start
                        cycle = GetNumberOfCycle(position);
                        if (0 == cycle % 2f)//if pair
                            virtualPos = position - (cycle * (last.time - first.time));
                        else
                            virtualPos = last.time - position + first.time + (cycle * (last.time - first.time));
                        return GetCurvePosition(virtualPos);

                    case WrapMode.Linear:// linear y = a*x +b with a tangeant of last point
                        return first.value - MathHelper.RadiansToDegrees(first.inTangent) * (first.time - position);
                }
            }
            else if (position > last.time)
            {
                int cycle;
                switch (postMode)
                {
                    case WrapMode.Constant:
                        //constant
                        return last.value;

                    case WrapMode.Cycle:
                        //start -> end / start -> end
                        cycle = GetNumberOfCycle(position);
                        float virtualPos = position - (cycle * (last.time - first.time));
                        return GetCurvePosition(virtualPos);

                    case WrapMode.CycleOffset:
                        //make the curve continue (with no step) so must up the curve each cycle of delta(value)
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.time - first.time));
                        return (GetCurvePosition(virtualPos) + cycle * (last.value - first.value));

                    case WrapMode.Oscillate:
                        //go back on curve from end and target start
                        // start-> end / end -> start
                        cycle = GetNumberOfCycle(position);
                        virtualPos = position - (cycle * (last.time - first.time));
                        if (0 == cycle % 2f)//if pair
                            virtualPos = position - (cycle * (last.time - first.time));
                        else
                            virtualPos = last.time - position + first.time + (cycle * (last.time - first.time));
                        return GetCurvePosition(virtualPos);

                    case WrapMode.Linear:  // linear y = a*x +b with a tangeant of last point
                        return last.value + MathHelper.RadiansToDegrees(last.outTangent) * (position - last.time);
                }
            }

            //in curve
            return GetCurvePosition(position);
        }

        private int GetNumberOfCycle(float position)
        {
            float cycle = (position - keys[0].time) / (keys[keys.Length - 1].time - keys[0].time);
            if (cycle < 0f)
                cycle--;
            return (int)cycle;
        }

        private float GetCurvePosition(float time)
        {
            Keyframe prev = keys[0];
            Keyframe next;
            for (int i = 1; i < keys.Length; ++i)
            {
                next = Unsafe.Add(ref keys[0], i);

                if (next.time >= time)
                {
                    if (prev.outTangent is float.PositiveInfinity || next.inTangent is float.PositiveInfinity)
                        return prev.value;
                    float t = (time - prev.time) / (next.time - prev.time);//to have t in [0,1]

                    float dt = next.time - prev.time;
                    float m0 = prev.outTangent * dt;
                    float m1 = next.inTangent * dt;

                    float t2 = t * t;
                    float t3 = t2 * t;

                    float a = 2 * t3 - 3 * t2 + 1;
                    float b = t3 - 2 * t2 + t;
                    float c = t3 - t2;
                    float d = -2 * t3 + 3 * t2;

                    return a * prev.value + b * m0 + c * m1 + d * next.value;
                }
                prev = next;
            }
            return prev.value;
        }

        public int AddKey(ref Keyframe key)
        {
            Array.Resize(ref keys, keys.Length + 1);
            keys[keys.Length - 1] = key;
            Array.Sort(keys, new KeyframeComparer());
            return Array.IndexOf(keys, key);
        }

        public int MoveKey(int index, ref Keyframe key)
        {
            var newTime = key.time;
            if (Array.Exists(keys, (item) => item.time == newTime))
                key.time = Unsafe.Add(ref keys[0], index).time;
            keys[index] = key;
            Array.Sort(keys, new KeyframeComparer());
            return Array.IndexOf(keys, key);
        }

        public void RemoveKey(int index)
        {
            var stride = Unsafe.SizeOf<Keyframe>();
            ref var memAddr = ref Unsafe.As<Keyframe, byte>(ref keys[index]);
            ref var memAddr1 = ref Unsafe.Add(ref memAddr, stride);
            Unsafe.CopyBlock(ref memAddr, ref memAddr1, (uint)(stride * (keys.Length - index - 1)));
            Array.Resize(ref keys, keys.Length - 1);
        }

        public void SwapKeyframes(ref Keyframe key1, ref Keyframe key2)
        {
        }

        public void SmoothTangents(int index, float weight)
        {
            throw new NotImplementedException(nameof(SmoothTangents) + " not implemented yet");
        }
    }

    public struct Keyframe
    {
        public float time;
        public float value;
        public float inTangent;
        public float outTangent;
        public int tangentMode;

        public Keyframe(float t, float v, float inTan, float outTan)
        {
            time = t;
            value = v;
            inTangent = inTan;
            outTangent = outTan;
            tangentMode = (int)TangentMode.Editable;
        }
    }

    public enum WrapMode
    {
        Constant,
        Linear,
        Cycle,
        CycleOffset,
        Oscillate
    }

    internal class KeyframeComparer : IComparer<Keyframe>
    {
        public int Compare(Keyframe x, Keyframe y)
        {
            return x.time > y.time ? 1 : -1;
        }
    }
}