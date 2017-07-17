using System;
using System.Numerics;
using System.Collections.Generic;

namespace Sharp
{
    [Flags]
    internal enum NumericMode
    {//for width/height
        Prectange,
        Fill,//?
        Number
    }

    public static class NumericsExtensions
    {
        //public static Matrix4x4 ClearRotation(this Matrix4x4 m) {
        // m.Row0.Xyz = new Vector3(m.Row0.Xyz.Length, 0, 0);
        // m.Row1.Xyz = new Vector3(0, m.Row1.Xyz.Length, 0);
        // m.Row2.Xyz = new Vector3(0, 0, m.Row2.Xyz.Length);
        // return m;
        //}
    }
}