using System;
using System.Drawing;
using OpenTK.Graphics.OpenGL;

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
			
			GL.LineWidth (gizmoThick);
			GL.Enable (EnableCap.Blend);
			GL.Disable (EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			//GL.DepthFunc (DepthFunction.Always);
			DrawHelper.DrawLine(0, 0, 0, 0, 3f * gizmoScale, 0, yColor);
		//	glControl.SetPickingNames(translationWidget_Id, widgetY_Id);
			DrawHelper.DrawConeY(0.2f * gizmoScale, 0.5f * gizmoScale, 3f * gizmoScale);
			DrawHelper.DrawPlaneXZ(1f * gizmoScale, 0.3f * gizmoScale, yColor);

			DrawHelper.DrawLine(0, 0, 0, 3f * gizmoScale, 0, 0, xColor);
			DrawHelper.DrawConeX(0.2f * gizmoScale, 0.5f * gizmoScale, 3f * gizmoScale);
			DrawHelper.DrawPlaneZY(1f * gizmoScale, 0.3f * gizmoScale, xColor);

			DrawHelper.DrawLine(0, 0, 0, 0, 0, 3f * gizmoScale, zColor);
			DrawHelper.DrawConeZ(0.2f * gizmoScale, 0.5f * gizmoScale, 3f * gizmoScale);
			DrawHelper.DrawPlaneYX(1f * gizmoScale, 0.3f * gizmoScale, zColor);
			GL.Color4 (System.Drawing.Color.White);
			GL.LineWidth (1);
			GL.Enable (EnableCap.DepthTest);
			//GL.DepthFunc (DepthFunction.Less);
		}
		public static void DrawRotateGizmo(){
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
			GL.LineWidth (gizmoThick);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.DepthFunc (DepthFunction.Always);
			// Y
			DrawHelper.DrawCircleY(3 * gizmoScale, 3f, yColor);
			// Z
			DrawHelper.DrawCircleZ(3 * gizmoScale, 3f, zColor);
			// X
			DrawHelper.DrawCircleX(3 * gizmoScale, 3f, xColor);
			GL.LineWidth (1);
			GL.DepthFunc (DepthFunction.Less);
		}
		public static void DrawScaleGizmo(){
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
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
		}
	
	}
}

