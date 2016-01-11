using System;
using OpenTK;
using Sharp;

namespace Sharp
{
	[Serializable]
	public struct BoundingBox /*: IEquatable<BoundingBox>*/
	{
		#region Public Fields

		public Vector3 Min;
		public Vector3 Max;

		#endregion Public Fields

		public BoundingBox(Vector3 min, Vector3 max)
		{
			this.Min = min;
			this.Max = max;
		}
		public Vector3[] GetCorners()
		{
			return new Vector3[] {
				new Vector3(this.Min.X, this.Max.Y, this.Max.Z), 
				new Vector3(this.Max.X, this.Max.Y, this.Max.Z),
				new Vector3(this.Max.X, this.Min.Y, this.Max.Z), 
				new Vector3(this.Min.X, this.Min.Y, this.Max.Z), 
				new Vector3(this.Min.X, this.Max.Y, this.Min.Z),
				new Vector3(this.Max.X, this.Max.Y, this.Min.Z),
				new Vector3(this.Max.X, this.Min.Y, this.Min.Z),
				new Vector3(this.Min.X, this.Min.Y, this.Min.Z)
			};
		}


		public override int GetHashCode()
		{
			return this.Min.GetHashCode() + this.Max.GetHashCode();
		}
		public Vector3 getPositiveVertex( Vector3 normal, Matrix4 modelMatrix )
		{
			Vector3 positiveVertex = Min;// add /scale

			if( normal.X >= 0.0f ) positiveVertex.X =Max.X;
			if( normal.Y >= 0.0f ) positiveVertex.Y =Max.Y;
			if( normal.Z >= 0.0f ) positiveVertex.Z =Max.Z;

			return Vector3.TransformPosition(positiveVertex, modelMatrix);
		}

		public Vector3 getNegativeVertex( Vector3 normal, Matrix4 modelMatrix )
		{
			Vector3 negativeVertex =Max;

			if( normal.X >= 0.0f ) negativeVertex.X =Min.X;
			if( normal.Y >= 0.0f ) negativeVertex.Y =Min.Y;
			if( normal.Z >= 0.0f ) negativeVertex.Z =Min.Z;

			return Vector3.TransformPosition(negativeVertex, modelMatrix);
		}
		public bool Intersect(ref Ray ray, ref Matrix4 matrix)
		{
			var max=Vector3.TransformPosition(Max,matrix);
			var min=Vector3.TransformPosition(Min,matrix);
			//first test if start in box
			if (ray.origin.X >= min.X
				&& ray.origin.X <= max.X
				&& ray.origin.Y >= min.Y
				&& ray.origin.Y <= max.Y
				&& ray.origin.Z >= min.Z
				&& ray.origin.Z <= max.Z)
				return true;// here we concidere cube is full and origine is in cube so intersect at origine


			//Second we check each face
			Vector3 maxT = new Vector3(-1.0f);
			//Vector3 minT = new Vector3(-1.0f);
			//calcul intersection with each faces
			if (ray.origin.X < min.X && ray.direction.X != 0.0f)
				maxT.X = (min.X - ray.origin.X) / ray.direction.X;
			else if (ray.origin.X > max.X && ray.direction.X != 0.0f)
				maxT.X = (max.X - ray.origin.X) / ray.direction.X;
			if (ray.origin.Y < min.Y && ray.direction.Y != 0.0f)
				maxT.Y = (min.Y - ray.origin.Y) / ray.direction.Y;
			else if (ray.origin.Y > max.Y && ray.direction.Y != 0.0f)
				maxT.Y = (max.Y - ray.origin.Y) / ray.direction.Y;
			if (ray.origin.Z < min.Z && ray.direction.Z != 0.0f)
				maxT.Z = (min.Z - ray.origin.Z) / ray.direction.Z;
			else if (ray.origin.Z > max.Z && ray.direction.Z != 0.0f)
				maxT.Z = (max.Z - ray.origin.Z) / ray.direction.Z;


			//get the maximum maxT
			if (maxT.X > maxT.Y && maxT.X > maxT.Z)
			{
				if (maxT.X < 0.0f)
					return false;// ray go on opposite of face
				//coordonate of hit point of face of cube
				float coord = ray.origin.Z + maxT.X * ray.direction.Z;
				// if hit point coord ( intersect face with ray) is out of other plane coord it miss 
				if (coord < min.Z || coord > max.Z)
					return false;
				coord = ray.origin.Y + maxT.X *ray.direction.Y;
				if (coord < min.Y || coord > max.Y)
					return false;
				return true;
			}
			if (maxT.Y > maxT.X && maxT.Y > maxT.Z)
			{
				if (maxT.Y < 0.0f)
					return false;// ray go on opposite of face
				//coordonate of hit point of face of cube
				float coord = ray.origin.Z + maxT.Y * ray.direction.Z;
				// if hit point coord ( intersect face with ray) is out of other plane coord it miss 
				if (coord < min.Z || coord > max.Z)
					return false;
				coord =ray.origin.X + maxT.Y * ray.direction.X;
				if (coord < min.X || coord > max.X)
					return false;
				return true;
			}
			else //Z
			{
				if (maxT.Z < 0.0f)
					return false;// ray go on opposite of face
				//coordonate of hit point of face of cube
				float coord =ray.origin.X + maxT.Z * ray.direction.X;
				// if hit point coord ( intersect face with ray) is out of other plane coord it miss 
				if (coord < min.X || coord > max.X)
					return false;
				coord = ray.origin.Y + maxT.Z * ray.direction.Y;
				if (coord < min.Y || coord > max.Y)
					return false;
				return true;
			}
		}
		/// <summary>
		/// Determines whether there is an intersection between a <see cref="SharpDX.Ray"/> and a <see cref="SharpDX.BoundingSphere"/>.
		/// </summary>
		/// <param name="ray">The ray to test.</param>
		/// <param name="sphere">The sphere to test.</param>
		/// <param name="distance">When the method completes, contains the distance of the intersection,
		/// or 0 if there was no intersection.</param>
		/// <returns>Whether the two objects intersected.</returns>
		public static bool Intersect(ref Ray ray, ref Vector3 centerSphere,float radius,ref Matrix4 matrix, out float distance)
		{
			//Source: Real-Time Collision Detection by Christer Ericson
			//Reference: Page 177
			var center=Vector3.TransformPosition(centerSphere,matrix);
			Vector3 m=ray.origin-center;

			float b = Vector3.Dot(m, ray.direction);
			float c = Vector3.Dot(m, m) - (radius * radius);

			if (c > 0f && b > 0f)
			{
				distance = 0f;
				return false;
			}

			float discriminant = b * b - c;

			if (discriminant < 0f)
			{
				distance = 0f;
				return false;
			}

			distance = -b - (float)Math.Sqrt(discriminant);

			if (distance < 0f)
				distance = 0f;

			return true;
		}

		/// <summary>
		/// Determines whether there is an intersection between a <see cref="SharpDX.Ray"/> and a <see cref="SharpDX.BoundingSphere"/>. 
		/// </summary>
		/// <param name="ray">The ray to test.</param>
		/// <param name="sphere">The sphere to test.</param>
		/// <param name="point">When the method completes, contains the point of intersection,
		/// or <see cref="SharpDX.Vector3.Zero"/> if there was no intersection.</param>
		/// <returns>Whether the two objects intersected.</returns>
		public static bool Intersect(ref Ray ray, ref Vector3 centerSphere,float radius,ref Matrix4 matrix, out Vector3 point)
		{
			float distance;
			if (!Intersect(ref ray, ref centerSphere, radius,ref matrix, out distance))
			{
				point = Vector3.Zero;
				return false;
			}

			point = ray.origin + (ray.direction * distance);
			return true;
		}

		 
	}
}   


