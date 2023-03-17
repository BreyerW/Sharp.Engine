using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
		public const float Deg2Rad = MathF.PI / 180f;

		// Radians-to-degrees conversion constant (RO).
		public const float Rad2Deg = 180.0f / MathF.PI;

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
		public static Vector3 NormalizeAngles(Vector3 angles)
		{
			float halfPi = (MathF.PI / 2f);
			return new Vector3((angles.X + halfPi) % (2 * halfPi) - halfPi, (angles.Y + MathF.PI) % (2 * MathF.PI) - MathF.PI, (angles.Z + MathF.PI) % (2 * MathF.PI) - MathF.PI);
		}
		public static Vector3 NormalizeEulerAngles(Vector3 euler)
		{
			float twopi = 2f * MathF.PI;

			// Normalize yaw to [-π, π]
			float yaw = (euler.Y % twopi + twopi) % twopi;
			if (yaw > MathF.PI) yaw -= twopi;

			// Normalize pitch to [-π/2, π/2]
			float pitch = (euler.X % twopi + twopi) % twopi;
			if (pitch > MathF.PI) pitch -= twopi;
			//if (pitch > MathF.PI / 2) pitch = MathF.PI - pitch;
			//if (pitch < -MathF.PI / 2) pitch = -MathF.PI - pitch;

			// Normalize roll to [-π, π]
			float roll = (euler.Z % twopi + twopi) % twopi;
			if (roll > MathF.PI) roll -= twopi;

			return new Vector3(pitch, yaw, roll);
		}
		public static Vector3 FindClosestEuler(Vector3 eul, Vector3 hint)
		{
			/* we could use M_PI as pi_thresh: which is correct but 5.1 gives better results.
			 * Checked with baking actions to fcurves - campbell */
			const float pi_thresh = (5.1f);
			const float pi_x2 = (2.0f * MathF.PI);
			var copy = Vector3.Zero;
			var dif = MemoryMarshal.CreateSpan(ref copy.X, 3);
			var seul = MemoryMarshal.CreateSpan(ref eul.X, 3);
			var shint = MemoryMarshal.CreateSpan(ref hint.X, 3);
			/* correct differences of about 360 degrees first */
			for (int i = 0; i < 3; i++)
			{
				dif[i] = seul[i] - shint[i];
				if (dif[i] > pi_thresh)
				{
					seul[i] -= MathF.Floor((dif[i] / pi_x2) + 0.5f) * pi_x2;
					dif[i] = seul[i] - shint[i];
				}
				else if (dif[i] < -pi_thresh)
				{
					seul[i] += MathF.Floor((-dif[i] / pi_x2) + 0.5f) * pi_x2;
					dif[i] = seul[i] - shint[i];
				}
			}

			/* is 1 of the axis rotations larger than 180 degrees and the other small? NO ELSE IF!! */
			if (MathF.Abs(dif[0]) > 3.2f && MathF.Abs(dif[1]) < 1.6f && MathF.Abs(dif[2]) < 1.6f)
			{
				if (dif[0] > 0.0f)
				{
					seul[0] -= pi_x2;
				}
				else
				{
					seul[0] += pi_x2;
				}
			}
			if (MathF.Abs(dif[1]) > 3.2f && MathF.Abs(dif[2]) < 1.6f && MathF.Abs(dif[0]) < 1.6f)
			{
				if (dif[1] > 0.0f)
				{
					seul[1] -= pi_x2;
				}
				else
				{
					seul[1] += pi_x2;
				}
			}
			if (MathF.Abs(dif[2]) > 3.2f && MathF.Abs(dif[0]) < 1.6f && MathF.Abs(dif[1]) < 1.6f)
			{
				if (dif[2] > 0.0f)
				{
					seul[2] -= pi_x2;
				}
				else
				{
					seul[2] += pi_x2;
				}
			}
			return eul;
		}
		public static Vector3 ToEulerAngles(this in Quaternion q)
		{
			float sqw = q.W * q.W;
			float sqx = q.X * q.X;
			float sqy = q.Y * q.Y;
			float sqz = q.Z * q.Z;

			float yaw, pitch, roll;
			float test = q.X * q.Y + q.Z * q.W;
			if (test > 0.49999f)
			{
				// singularity at north pole
				yaw = 2.0f * MathF.Atan2(q.X, q.W);
				pitch = MathF.PI / 2.0f;
				roll = 0.0f;
			}
			else if (test < -0.49999f)
			{
				// singularity at south pole
				yaw = -2.0f * MathF.Atan2(q.X, q.W);
				pitch = -MathF.PI / 2.0f;
				roll = 0.0f;
			}
			else
			{
				yaw = MathF.Atan2(2.0f * q.Y * q.W - 2.0f * q.X * q.Z, sqx - sqy - sqz + sqw);
				pitch = MathF.Asin(2.0f * test);
				roll = MathF.Atan2(2.0f * q.X * q.W - 2.0f * q.Y * q.Z, -sqx + sqy - sqz + sqw);
			}
			return new Vector3(pitch, roll, yaw);
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