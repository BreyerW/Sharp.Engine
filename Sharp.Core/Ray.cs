using System;
using System.Numerics;

namespace Sharp
{
    public struct Ray
    {
        public Vector3 origin;
        public Vector3 direction;

        public Ray(Vector3 orig, Vector3 dir)
        {
            origin = orig;
            direction = dir;
        }

        public float IntersectPlane(Vector4 plane)
        {
            float numer = Vector3.Dot(new Vector3(plane.X, plane.Y, plane.Z), origin) - plane.W;
            float denom = Vector3.Dot(new Vector3(plane.X, plane.Y, plane.Z), direction);

            if (Math.Abs(denom) < float.Epsilon)  // normal is orthogonal to vector, cant intersect
                return -1.0f;

            return -(numer / denom);
        }
    }
}