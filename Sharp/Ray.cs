using System;
using OpenTK;

namespace Sharp
{
	public struct Ray
	{
		public Vector3 origin;
		public Vector3 direction;

		public Ray(Vector3 orig, Vector3 dir){
			origin = orig;
			direction = dir;
		}
	}
}

