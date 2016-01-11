using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using Sharp.Editor.Views;

namespace Sharp.Editor
{
	public static class Manipulators
	{
		public static readonly Color xColor=Color.Red;
		public static readonly Color yColor=Color.Blue;
		public static readonly Color zColor=Color.LimeGreen;
		public static float gizmoScale=10f;
		public static float gizmoThick=2.5f;

		public static void DrawTranslateGizmo(){
			MainEditorView.editorBackendRenderer.DrawTranslateGizmo (gizmoThick,gizmoScale,xColor,yColor,zColor);
		}
		public static void DrawRotateGizmo(){
			MainEditorView.editorBackendRenderer.DrawRotateGizmo (gizmoThick,gizmoScale,xColor,yColor,zColor);
		}
		/*public static void DrawScaleGizmo(){
			SetupGraphic();
			GL.LineWidth (gizmoThick);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.DepthFunc (DepthFunction.Lequal);

			DrawHelper.DrawLine(0, 0, 0, 3f * gizmoScale, 0, 0, yColor);
			DrawHelper.DrawCube(0.25f * gizmoScale, 0, 3.125f * gizmoScale, 0, yColor);

			DrawHelper.DrawLine(0, 0, 0, 3f * gizmoScale, 0, 0, zColor);
			DrawHelper.DrawCube(0.25f * gizmoScale, 0, 0, 3.125f * gizmoScale, zColor);

			DrawHelper.DrawLine(0, 0, 0, 3f * gizmoScale, 0, 0, xColor);
			DrawHelper.DrawCube(0.25f * gizmoScale, 3.125f * gizmoScale, 0, 0, xColor);

			DrawHelper.DrawCube(0.5f * gizmoScale, 0, 0, 0, Color.White);
			GL.LineWidth (1);
			GL.DepthFunc (DepthFunction.Less);
		}*/
	
	}
}

