using Newtonsoft.Json;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Engine.Components
{
	public class Transform : Component//1 ui szablon dla roznych typow w inspectorze
	{
		private readonly static int mat4x4Stride = Marshal.SizeOf<Matrix4x4>();
		[NonSerializable]
		private Matrix4x4 modelMatrix;
		internal Vector3 position = Vector3.Zero;
		internal Vector3 rotation = Vector3.Zero;
		internal Vector3 scale = Vector3.One;
		[NonSerializable, JsonIgnore]
		public ref readonly Matrix4x4 ModelMatrix
		{
			get
			{ /*unsafe { return ref Unsafe.AsRef<Matrix4x4>(modelMatrix.ToPointer()); }*/
				return ref modelMatrix;
			}
		}

		public Action onTransformChanged;

		public Vector3 Position
		{
			get
			{
				return position;
			}
			set
			{
				position = value;
				SetModelMatrix();
				onTransformChanged?.Invoke();
			}
		}

		public Vector3 Rotation
		{
			get
			{
				return rotation;
			}
			set
			{
				rotation = value;
				SetModelMatrix();
				onTransformChanged?.Invoke();
			}
		}

		public Vector3 Scale
		{
			get
			{
				return scale;
			}
			set
			{
				scale = value;
				SetModelMatrix();
				onTransformChanged?.Invoke();
			}
		}
		private void SetModelMatrix()
		{
			var angles = rotation * NumericsExtensions.Pi / 180f;
			/*unsafe
			{
				Unsafe.AsRef<Matrix4x4>(modelMatrix.ToPointer()) = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
			}*/
			modelMatrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
		}
		public Transform(Entity parent) : base(parent)
		{
			//modelMatrix = Marshal.AllocHGlobal(mat4x4Stride);
			parent.transform = this;
		}
	}
}