using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

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
		// Degrees-to-radians conversion constant (RO).
		public const float Deg2Rad = MathF.PI * 2f / 360f;

		// Radians-to-degrees conversion constant (RO).
		public const float Rad2Deg = 1f / Deg2Rad;

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
		public static Vector3 ToEulerAngles(this Quaternion q)
		{
			// Extract the quaternion components
			float qx = q.X;
			float qy = q.Y;
			float qz = q.Z;
			float qw = q.W;

			// Calculate the Euler angles
			float roll, pitch, yaw;

			float sinr_cosp = 2.0f * (qw * qx + qy * qz);
			float cosr_cosp = 1.0f - 2.0f * (qx * qx + qy * qy);
			roll = MathF.Atan2(sinr_cosp, cosr_cosp);

			float sinp = 2.0f * (qw * qy - qz * qx);
			if (MathF.Abs(sinp) >= 1.0f)
				pitch = MathF.PI / 2.0f * MathF.Sign(sinp);
			else
				pitch = MathF.Asin(sinp);

			float siny_cosp = 2.0f * (qw * qz + qx * qy);
			float cosy_cosp = 1.0f - 2.0f * (qy * qy + qz * qz);
			yaw = MathF.Atan2(siny_cosp, cosy_cosp);

			// Return the Euler angles as a Vector3
			return new Vector3(pitch, yaw, roll);
		}

		public static Matrix4x4 Inverted(in this Matrix4x4 m)
		{
			if (m.GetDeterminant() != 0)
			{
				Matrix4x4.Invert(m, out var invertedMat);
				return invertedMat;
			}
			return m;
		}
		public static Vector3 Forward(in this Matrix4x4 m)
		{
			return Vector3.Normalize(new Vector3(m.M31, m.M32, m.M33));
		}
		public static Vector3 Up(in this Matrix4x4 m)
		{
			return Vector3.Normalize(new(m.M21, m.M22, m.M23));
		}
		public static Vector3 Right(in this Matrix4x4 m)
		{
			return Vector3.Normalize(new(m.M11, m.M12, m.M13));
		}
		public static void DecomposeDirections(in this Matrix4x4 mat, out Vector3 right, out Vector3 up, out Vector3 forward)
		{
			right = Vector3.Normalize(new Vector3(mat.M11, mat.M12, mat.M13));
			up = Vector3.Normalize(new Vector3(mat.M21, mat.M22, mat.M23));
			forward = Vector3.Normalize(new Vector3(mat.M31, mat.M32, mat.M33));
		}
		public static Vector4 Normalized(in this Vector4 vec)
		{
			return Vector4.Normalize(vec);
		}

		public static Vector3 Normalized(in this Vector3 vec)
		{
			return Vector3.Normalize(vec);
		}

		public static Vector2 Normalized(in this Vector2 vec)
		{
			return Vector2.Normalize(vec);
		}

		public static Vector4 Transformed(in this Vector4 vec, in Matrix4x4 mat)
		{
			return Vector4.Transform(vec, mat);
		}

		public static Vector3 Transformed(in this Vector3 vec, in Quaternion quat)
		{
			return Vector3.Transform(vec, quat);
		}

		public static Vector3 Transformed(in this Vector3 vec, in Matrix4x4 mat)
		{
			return Vector3.Transform(vec, mat);
		}

		public static ref Vector4 Normalize(in this Vector4 vec)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector4.Normalize(vec);
			return ref refVec;
		}

		public static ref Vector3 Normalize(in this Vector3 vec)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector3.Normalize(vec);
			return ref refVec;
		}

		public static ref Vector2 Normalize(in this Vector2 vec)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector2.Normalize(vec);
			return ref refVec;
		}

		public static ref Vector4 Transform(in this Vector4 vec, in Matrix4x4 mat)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector4.Transform(vec, mat);
			return ref refVec;
		}

		public static ref Vector3 Transform(in this Vector3 vec, in Quaternion quat)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector3.Transform(vec, quat);
			return ref refVec;
		}

		public static ref Vector3 Transform(in this Vector3 vec, in Matrix4x4 mat)
		{
			ref var refVec = ref Unsafe.AsRef(vec);
			refVec = Vector3.Transform(vec, mat);
			return ref refVec;
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