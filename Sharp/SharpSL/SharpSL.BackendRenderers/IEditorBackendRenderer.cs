using System;
using OpenTK;
using System.Drawing;
using SharpAsset;

namespace SharpSL
{
	public interface IEditorBackendRenderer
	{
		void update (ref Skeleton skele);
		void newinit (ref Skeleton skele);
		void display (ref Skeleton skele);

		void DrawGrid(Color color,Vector3 pos, float X, float Y,ref Matrix4 projMat, int cell_size = 16, int grid_size = 2560);//IBackendRendererHelper/IEditorBackend?
		void DrawBox(Vector3 pos1, Vector3 pos2);
		void DrawRectangle(Vector3 pos1, Vector3 pos2);
		void DrawCone(float width, float height, float offset,Vector3 axis);
		void DrawConeX(float width, float height, float offset);
		void DrawConeY(float width, float height, float offset);
		void DrawConeZ(float width, float height, float offset);
		void DrawPlaneXZ(float size, float sizeOffset, Color unColor);
		void DrawPlaneZY(float size, float sizeOffset, Color unColor);
		void DrawPlaneYX(float size, float sizeOffset, Color unColor);
		void DrawSphere (float radius, int lats, int longs, Color unColor);
		void DrawCircleX (float size, float lineWidth, Color unColor);
		void DrawCircleY (float size, float lineWidth, Color unColor);
		void DrawCircleZ (float size, float lineWidth, Color unColor);
		void DrawSelectionSquare (float x1, float y1, float x2, float y2, Color unColor);
		void DrawLine (float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor);
		void DrawTranslateGizmo (float thickness, float scale, Color xColor,Color yColor,Color zColor);
		void DrawRotateGizmo (float thickness, float scale, Color xColor,Color yColor,Color zColor);
		void LoadMatrix(ref Matrix4 mat);
		void UnloadMatrix();
	}
}

