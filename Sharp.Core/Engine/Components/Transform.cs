using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Sharp.Engine.Components
{
	public class Transform : Component
	{
		private readonly static int mat4x4Stride = Marshal.SizeOf<Matrix4x4>();
		[JsonInclude]
		private Matrix4x4 modelMatrix = Matrix4x4.Identity;
		[JsonInclude]
		private Vector3 position = Vector3.Zero;
		[JsonInclude]
		private Vector3 eulerAngles = Vector3.Zero;
		[JsonInclude]
		private Vector3 scale = Vector3.One;
		//[JsonProperty]
		//private Quaternion rotation;

		//idea: ref returning getter that returns ref struct with access to transform position, rot, scale and matrix if ref setter in them will be supported to fire events while all these properties in transform will be private 
		[NonSerializable, JsonIgnore]
		public ref Matrix4x4 ModelMatrix
		{
			get
			{ /*unsafe { return ref Unsafe.AsRef<Matrix4x4>(modelMatrix.ToPointer()); }*/
				return ref modelMatrix;
			}
		}
		[JsonInclude]
		public Action onTransformChanged { get; set; }

		public Vector3 Position
		{
			get
			{
				//Matrix4x4.Decompose(modelMatrix, out _, out _, out var pos);
				return position;
			}
			internal set
			{
				position = value;
			}
		}
		public Vector3 Rotation
		{
			get => eulerAngles;
			internal set => eulerAngles = value;

		}

		/*[JsonIgnore]
		public Quaternion Rotation
		{
			get
			{
				//Matrix4x4.Decompose(modelMatrix, out _, out var rot, out _);
				return rotation;
			}
			set
			{
				if (rotation == value) return;
				var delta = value *Quaternion.Inverse(rotation);
				eulerAngles += delta.ToEulerAngles() * NumericsExtensions.Rad2Deg;
				ModelMatrix =Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(delta) * Matrix4x4.CreateTranslation(position);  //Matrix4x4.CreateFromQuaternion(delta) * ModelMatrix; //
				rotation = value;
			}
		}*/

		public Vector3 Scale
		{
			get
			{
				//Matrix4x4.Decompose(modelMatrix, out var s, out _, out _);
				return scale;
			}
			internal set
			{
				scale = value;
			}
		}
		//modelMatrix = Marshal.AllocHGlobal(mat4x4Stride);
		public Transform(Entity p) : base(p)
		{
		}
	}
}