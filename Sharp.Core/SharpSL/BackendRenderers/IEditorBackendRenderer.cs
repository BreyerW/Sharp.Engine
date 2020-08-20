using System;
using System.Numerics;
using SharpAsset;

namespace SharpSL
{
	public interface IEditorBackendRenderer
	{
		void update(ref Skeleton skele);

		void newinit(ref Skeleton skele);

		void display(ref Skeleton skele);

		void DrawGrid(ref float color, Vector3 pos, float X, float Y, ref Matrix4x4 projMat, int cell_size = 16, int grid_size = 2560);//IBackendRendererHelper/IEditorBackend?

		void DrawFilledPolyline(float size, float lineWidth, ref float color, ref Matrix4x4 mat, ref Vector3[] vecArray, bool fan = true);

		void DrawSlicedQuad(float x1, float y1, float x2, float y2, float left, float right, float top, float bottom, ref float unColor);

		void DrawTexturedQuad(float x1, float y1, float x2, float y2, ref float unColor);

		void DrawQuad(float x1, float y1, float x2, float y2, ref float unColor);

		void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, ref float unColor, float width = 1f);

		void DrawLine(Vector3 v1, Vector3 v2, ref float unColor);

		void LoadMatrix(ref Matrix4x4 mat);

		void UnloadMatrix();
	}
}