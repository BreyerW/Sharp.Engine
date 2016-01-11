using System;
using Sharp.Editor.Views;
using OpenTK;
using System.Drawing;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		public static void DrawGrid (System.Drawing.Color color,Vector3 pos, float X, float Y, int cell_size = 16, int grid_size = 2560)
		{
			MainEditorView.editorBackendRenderer.DrawGrid (color,pos,X,Y,cell_size,grid_size);
		}
		public static void DrawBox(Vector3 pos1, Vector3 pos2)
		{
			MainEditorView.editorBackendRenderer.DrawBox (pos1,pos2);
		}
		public static void DrawRectangle(Vector3 pos1, Vector3 pos2){
			MainEditorView.editorBackendRenderer.DrawRectangle (pos1, pos2);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawCone(float width, float height, float offset,Vector3 axis)
		{
			MainEditorView.editorBackendRenderer.DrawCone (width, height, offset, axis);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeY(float width, float height, float offset)
		{
			MainEditorView.editorBackendRenderer.DrawConeY (width, height, offset);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeZ(float width, float height, float offset)
		{
			MainEditorView.editorBackendRenderer.DrawConeZ (width,height, offset);
		}

		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeX(float width, float height, float offset)
		{
			MainEditorView.editorBackendRenderer.DrawConeX (width, height, offset);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneXZ(float size, float sizeOffset, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawPlaneXZ (size, sizeOffset, unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneZY(float size, float sizeOffset, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawPlaneZY (size, sizeOffset, unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneYX(float size, float sizeOffset, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawPlaneYX (size, sizeOffset,unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/ 
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="lats"></param>
		/// <param name="longs"></param>
		public static void DrawSphere(float radius, int lats, int longs, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawSphere (radius,lats,longs,unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleY(float size, float lineWidth, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawCircleY (size,lineWidth,unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleX(float size, float lineWidth, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawCircleX (size, lineWidth,unColor);
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleZ(float size, float lineWidth, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawCircleZ (size,lineWidth, unColor);
		}
		public static void DrawSelectionSquare(float x1, float y1, float x2, float y2, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawSelectionSquare (x1, y1, x2, y2,unColor);
		}
		public static void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawLine (v1x,v1y, v1z, v2x, v2y, v2z, unColor);
		}
	}
}

