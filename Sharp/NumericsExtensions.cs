using System;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;

namespace Sharp
{
    [Flags]
    internal enum NumericMode
    {//for width/height
        Prectange,
        Fill,//?
        Number,
        Auto
    }

    public static class NumericsExtensions
    {
        /* public static Matrix4x4 ClearRotation(in this Matrix4x4 m) {
          m.Row0.Xyz = new Vector3(m.Row0.Xyz.Length, 0, 0);
          m.Row1.Xyz = new Vector3(0, m.Row1.Xyz.Length, 0);
          m.Row2.Xyz = new Vector3(0, 0, m.Row2.Xyz.Length);
          return m;
         }*/
        public const float Pi = 3.141592653589793238462643383279502884197169399375105820974944592307816406286208998628034825342117067982148086513282306647093844609550582231725359408128481117450284102701938521105559644622948954930382f;

        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        /// <param name="degrees">An angle in degrees</param>
        /// <returns>The angle expressed in radians</returns>
        public static float DegreesToRadians(float degrees)
        {
            const float degToRad = (float)Math.PI / 180.0f;
            return degrees * degToRad;
        }

        /// <summary>
        /// Convert radians to degrees
        /// </summary>
        /// <param name="radians">An angle in radians</param>
        /// <returns>The angle expressed in degrees</returns>
        public static float RadiansToDegrees(float radians)
        {
            const float radToDeg = 180.0f / (float)Math.PI;
            return radians * radToDeg;
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        public static int Clamp(int n, int min, int max)
        {
            return Math.Max(Math.Min(n, max), min);
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        public static float Clamp(float n, float min, float max)
        {
            return Math.Max(Math.Min(n, max), min);
        }

        /// <summary>
        /// Clamps a number between a minimum and a maximum.
        /// </summary>
        /// <param name="n">The number to clamp.</param>
        /// <param name="min">The minimum allowed value.</param>
        /// <param name="max">The maximum allowed value.</param>
        /// <returns>min, if n is lower than min; max, if n is higher than max; n otherwise.</returns>
        public static double Clamp(double n, double min, double max)
        {
            return Math.Max(Math.Min(n, max), min);
        }

        public static float NextPowerOfTwo(float n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            }
            return (float)Math.Pow(2, Math.Ceiling(Math.Log((double)n, 2)));
        }

        /// <summary>
        /// Returns the next power of two that is greater than or equal to the specified number.
        /// </summary>
        /// <param name="n">The specified number.</param>
        /// <returns>The next power of two.</returns>
        public static double NextPowerOfTwo(double n)
        {
            if (n < 0)
            {
                throw new ArgumentOutOfRangeException("n", "Must be positive.");
            }
            return Math.Pow(2, Math.Ceiling(Math.Log((double)n, 2)));
        }

        public static void Invert(in this Matrix4x4 m, out Matrix4x4 outMat)
        {
            if (m.GetDeterminant() != 0)
                Matrix4x4.Invert(m, out outMat);
            else outMat = m;
        }

        public static void Normalize(in this Vector4 vec, out Vector4 outVec)
        {
            outVec = Vector4.Normalize(vec);
        }

        public static void Normalize(in this Vector3 vec, out Vector3 outVec)
        {
            outVec = Vector3.Normalize(vec);
        }

        public static void Normalize(in this Vector2 vec, out Vector2 outVec)
        {
            outVec = Vector2.Normalize(vec);
        }

        public static void Transform(in this Vector4 vec, in Matrix4x4 mat, out Vector4 outVec)
        {
            outVec = Vector4.Transform(vec, mat);
        }

        public static void Transform(in this Vector3 vec, in Quaternion quat, out Vector3 outVec)
        {
            outVec = Vector3.Transform(vec, quat);
        }

        public static void Transform(in this Vector3 vec, in Matrix4x4 mat, out Vector3 outVec)
        {
            outVec = new Vector3(
                vec.X * mat.M11 + vec.Y * mat.M21 + vec.Z * mat.M31,
                vec.X * mat.M12 + vec.Y * mat.M22 + vec.Z * mat.M32,
vec.X * mat.M13 + vec.Y * mat.M23 + vec.Z * mat.M33);
        }

        public static IEnumerable<(int index, T item)> Indexed<T>(this IEnumerable<T> enumeration)
        {
            var index = 0;
            foreach (var item in enumeration)
                yield return (index++, item);
        }

        public static void Deconstruct<T, U>(this KeyValuePair<T, U> k, out T t, out U u)
        {
            t = k.Key;
            u = k.Value;
        }

        /// <summary>Calculates the angle (in radians) between two vectors.</summary>
        /// <param name="first">The first vector.</param>
        /// <param name="second">The second vector.</param>
        /// <param name="result">Angle (in radians) between the vectors.</param>
        /// <remarks>Note that the returned angle is never bigger than the constant Pi.</remarks>
        public static float CalculateAngle(in Vector3 first, in Vector3 second)
        {
            return (float)Math.Acos(Clamp(Vector3.Dot(first, second) / (first.Length() * second.Length()), -1.0, 1.0));
        }
    }
}