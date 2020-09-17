using Newtonsoft.Json;
using Sharp.Core.Editor;
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
		public Vector3 position = Vector3.Zero;
		public Vector3 rotation = Vector3.Zero;
		public Vector3 scale = Vector3.One;
		[NonSerializable, JsonIgnore]
		public ref readonly Matrix4x4 ModelMatrix
		{
			get
			{ /*unsafe { return ref Unsafe.AsRef<Matrix4x4>(modelMatrix.ToPointer()); }*/
				return ref modelMatrix;
			}
		}
		[JsonProperty]
		public Action onTransformChanged { get; set; }

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
		public void SetModelMatrix()
		{
			var angles = rotation * NumericsExtensions.Pi / 180f;
			/*unsafe
			{
				Unsafe.AsRef<Matrix4x4>(modelMatrix.ToPointer()) = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
			}*/
			modelMatrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
		}
		//modelMatrix = Marshal.AllocHGlobal(mat4x4Stride);
		public Transform() : base()
		{
			onTransformChanged += () => Console.WriteLine("transform changed");
		}
	}
}