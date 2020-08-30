
using SharpAsset;
using System.Numerics;

namespace Sharp.Editor
{
	public class EditorCamera//TODO: merge this camera with Camera 
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
		public void SetOrthoMatrix(int width, int height)
		{
			OrthoMatrix = Matrix4x4.CreateOrthographicOffCenter(0, width, height, 0, -1, 1); //Matrix4.CreateOrthographic(width, height, ZNear, ZFar);
			Material.BindGlobalProperty("camOrtho", OrthoMatrix);
			this.width = width;
			this.height = height;
		}
	}
}
