using System;
using Sharp.Editor.Views;
using OpenTK;

namespace Sharp.Editor
{
    public static class DrawHelper
    {
        public static void DrawGrid(Color color, Vector3 pos, float X, float Y, ref Matrix4 projMat, int cell_size = 16, int grid_size = 2560)
        {
            MainEditorView.editorBackendRenderer.DrawGrid(ref color.R, pos, X, Y, ref projMat, cell_size, grid_size);
        }

        public static void DrawBox(Vector3 pos1, Vector3 pos2)
        {
            MainEditorView.editorBackendRenderer.DrawBox(pos1, pos2);
        }

        public static void DrawRectangle(Vector3 pos1, Vector3 pos2)
        {
            MainEditorView.editorBackendRenderer.DrawRectangle(pos1, pos2);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="offset"></param>
        public static void DrawCone(float width, float height, float offset, Vector3 axis)
        {
            MainEditorView.editorBackendRenderer.DrawCone(width, height, offset, axis);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="offset"></param>
        public static void DrawConeY(float width, float height, float offset)
        {
            MainEditorView.editorBackendRenderer.DrawConeY(width, height, offset);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="offset"></param>
        public static void DrawConeZ(float width, float height, float offset)
        {
            MainEditorView.editorBackendRenderer.DrawConeZ(width, height, offset);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="offset"></param>
        public static void DrawConeX(float width, float height, float offset)
        {
            MainEditorView.editorBackendRenderer.DrawConeX(width, height, offset);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="size"></param>
        /// <param name="sizeOffset"></param>
        public static void DrawPlaneXZ(float size, float sizeOffset, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawPlaneXZ(size, sizeOffset, ref unColor.R);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="size"></param>
        /// <param name="sizeOffset"></param>
        public static void DrawPlaneZY(float size, float sizeOffset, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawPlaneZY(size, sizeOffset, ref unColor.R);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="size"></param>
        /// <param name="sizeOffset"></param>
        public static void DrawPlaneYX(float size, float sizeOffset, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawPlaneYX(size, sizeOffset, ref unColor.R);
        }

        /// <summary>
        /// From http://code.google.com/p/3d-editor-toolkit/
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="lats"></param>
        /// <param name="longs"></param>
        public static void DrawSphere(float radius, int lats, int longs, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawSphere(radius, lats, longs, ref unColor.R);
        }

        public static void DrawCircle(float lineWidth, float size, Color unColor, SharpSL.Plane plane, float angle = 360, bool filled = false)
        {
            MainEditorView.editorBackendRenderer.DrawCircle(size, lineWidth, ref unColor.R, plane, angle, filled);
        }

        public static void DrawSelectionSquare(float x1, float y1, float x2, float y2, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawSelectionSquare(x1, y1, x2, y2, ref unColor.R);
        }

        public static void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor)
        {
            MainEditorView.editorBackendRenderer.DrawLine(v1x, v1y, v1z, v2x, v2y, v2z, ref unColor.R);
        }

        public static void DrawTranslationGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor)
        {
            MainEditorView.editorBackendRenderer.DrawTranslateGizmo(thickness, scale, ref xColor.R, ref yColor.R, ref zColor.R);
        }

        public static void DrawRotationGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor)
        {
            MainEditorView.editorBackendRenderer.DrawRotateGizmo(thickness, scale, ref xColor.R, ref yColor.R, ref zColor.R);
        }

        public static void DrawScaleGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor, Vector3 offset = default(Vector3))
        {
            MainEditorView.editorBackendRenderer.DrawScaleGizmo(thickness, scale, ref xColor.R, ref yColor.R, ref zColor.R, offset);
        }

        public static void DrawFilledPolyline(float size, float lineWidth, Color color, ref Matrix4 mat, ref Vector3[] vecArray, bool fan = true)
        {
            MainEditorView.editorBackendRenderer.DrawFilledPolyline(size, lineWidth, ref color.R, ref mat, ref vecArray, fan);
        }
    }
}