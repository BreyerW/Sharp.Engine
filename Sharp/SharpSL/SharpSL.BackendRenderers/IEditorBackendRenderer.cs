using System;
using OpenTK;
using SharpAsset;

namespace SharpSL
{
    public enum Plane
    {
        X,
        Y,
        Z
    }

    public interface IEditorBackendRenderer
    {
        void update(ref Skeleton skele);

        void newinit(ref Skeleton skele);

        void display(ref Skeleton skele);

        void DrawGrid(ref byte color, Vector3 pos, float X, float Y, ref Matrix4 projMat, int cell_size = 16, int grid_size = 2560);//IBackendRendererHelper/IEditorBackend?

        void DrawBox(Vector3 pos1, Vector3 pos2);

        void DrawRectangle(Vector3 pos1, Vector3 pos2);

        void DrawCone(float width, float height, float offset, Vector3 axis);

        void DrawConeX(float width, float height, float offset);

        void DrawConeY(float width, float height, float offset);

        void DrawConeZ(float width, float height, float offset);

        void DrawPlaneXZ(float size, float sizeOffset, ref byte unColor);

        void DrawPlaneZY(float size, float sizeOffset, ref byte unColor);

        void DrawPlaneYX(float size, float sizeOffset, ref byte unColor);

        void DrawSphere(float radius, int lats, int longs, ref byte unColor);

        void DrawCircle(float size, float lineWidth, ref byte unColor, Plane plane, float angle, bool filled);

        void DrawFilledPolyline(float size, float lineWidth, ref byte color, ref Matrix4 mat, ref Vector3[] vecArray, bool fan = true);

        void DrawSlicedQuad(float x1, float y1, float x2, float y2, float left, float right, float top, float bottom, ref byte unColor);

        void DrawTexturedQuad(float x1, float y1, float x2, float y2, ref byte unColor);

        void DrawQuad(float x1, float y1, float x2, float y2, ref byte unColor);

        void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, ref byte unColor, float width = 1f);

        void DrawLine(Vector3 v1, Vector3 v2, ref byte unColor);

        void DrawTranslateGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor);

        void DrawRotateGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor);

        void DrawScaleGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor, Vector3 offset);

        void DrawDiamond(Vector2 pos, Vector2 dir, ref byte color);

        void LoadMatrix(ref Matrix4 mat);

        void UnloadMatrix();
    }
}