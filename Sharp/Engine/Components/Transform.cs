using System;
using System.Numerics;

namespace Sharp.Engine.Components
{
	public class Transform : Component//1 ui szablon dla roznych typow w inspectorze
	{
		[NonSerializable]
		private Matrix4x4 modelMatrix;
		internal Vector3 position = Vector3.Zero;
		internal Vector3 rotation = Vector3.Zero;
		internal Vector3 scale = Vector3.One;
		[NonSerializable]
		public ref readonly Matrix4x4 ModelMatrix
		{
			get { return ref modelMatrix; }
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
			modelMatrix = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateRotationX(angles.X) * Matrix4x4.CreateRotationY(angles.Y) * Matrix4x4.CreateRotationZ(angles.Z) * Matrix4x4.CreateTranslation(position);
		}
		public Transform(Entity parent) : base(parent)
		{
			parent.transform = this;
		}
	}
}