
using System.Numerics;

namespace Sharp.Editor
{
	public class EditorCamera
	{
		public int width;
		public int height;
		private Matrix4x4 orthoMatrix;
		public ref Matrix4x4 OrthoMatrix
		{
			get
			{
				return ref orthoMatrix;
			}
		}

		private Matrix4x4 orthoLeftBottomMatrix;
		public ref Matrix4x4 OrthoLeftBottomMatrix
		{
			get
			{
				return ref orthoLeftBottomMatrix;
			}
		}
		public void SetOrthoMatrix(int width, int height)
		{
			OrthoMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, 0, height, -1, 1); //Matrix4.CreateOrthographic(width, height, ZNear, ZFar);
			OrthoLeftBottomMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1); //Matrix4.CreateOrthographic(width, height, ZNear, ZFar);

			this.width = width;
			this.height = height;
		}
	}
}
