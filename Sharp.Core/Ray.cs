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

		public float IntersectPlane(Plane plane)
		{
			float numer = Vector3.Dot(plane.Normal, origin) - plane.D;
			float denom = Vector3.Dot(plane.Normal, direction);
			if (Math.Abs(denom) < float.Epsilon)  // normal is orthogonal to vector, cant intersect
				return -1.0f;

			return -(numer / denom);
		}
	}
}