using System;
using System.Numerics;
using SharpAsset;
using SharpAsset.AssetPipeline;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		private static Shader shader;
		internal static Material gridLineMaterial;
		internal static Material gizmoMaterial;
		internal static Material screenGizmoMaterial;
		public static float gridSize = 256;
		public static float cellSize = 16;


		static DrawHelper()
		{
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");

			ref var circleMesh = ref Pipeline.Get<Mesh>().GetAsset("gizmo");
			FillMaterial(out gizmoMaterial, shader, circleMesh, Manipulators.xColor);
			gizmoMaterial.BindProperty("highlightColor", new Color(0.75f, 0.75f, 0.75f, 0.75f));
			gizmoMaterial.BindProperty("enableHighlight", 0);

			ref var screenCircleMesh = ref Pipeline.Get<Mesh>().GetAsset("screen_circle");
			FillMaterial(out screenGizmoMaterial, shader, screenCircleMesh, Manipulators.screenRotColor);
			screenGizmoMaterial.BindProperty("highlightColor", new Color(0.75f, 0.75f, 0.75f, 0.75f));
			screenGizmoMaterial.BindProperty("enableHighlight", 0);

			var gridShader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GridEditorShader.shader");

			ref var gridMesh = ref Pipeline.Get<Mesh>().GetAsset("negative_square");
			FillMaterial(out gridLineMaterial, gridShader, gridMesh, Color.White);
			gridLineMaterial.BindProperty("width", 2f);

		}
		private static void FillMaterial(out Material mat, in Shader s, in Mesh m, in Color c)
		{
			mat = new Material();
			mat.BindShader(0, s);
			mat.BindProperty("mesh", m);
			mat.BindProperty("color", c);
		}
		public static void DrawGrid(Vector3 pos)
		{
			var scale = MathF.Abs(Camera.main.Parent.transform.Position.Y);
			gridSize = Camera.main.ZFar * (scale / 100f + 1);
			var gridMat = Matrix4x4.CreateTranslation(0, /*isTooFar ? Camera.main.Parent.transform.Position.Y - Camera.main.ZFar * 0.95f - 10 :*/ 0, 0);
			gridLineMaterial.BindProperty("len", gridSize);
			gridLineMaterial.BindProperty("model", gridMat);
			gridLineMaterial.Draw();
		}

		public static void DrawGizmo(in Matrix4x4 rotAndScaleMat, in Matrix4x4 alignToScreen)
		{
			gizmoMaterial.BindProperty("model", rotAndScaleMat);
			screenGizmoMaterial.BindProperty("model", alignToScreen);
			if (Manipulators.hoveredGizmoId is >= Gizmo.TranslateX)
			{
				if ((Manipulators.useUniformScale is false || (Manipulators.useUniformScale is true && Manipulators.hoveredGizmoId is not Gizmo.ScaleX and not Gizmo.ScaleY and not Gizmo.ScaleZ)) && Manipulators.hoveredGizmoId is not Gizmo.RotateScreen)
				{
					if (Manipulators.hoveredGizmoId is not Gizmo.TranslateX)
						gizmoMaterial.Draw(..(Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX - 1));

					gizmoMaterial.BindProperty("enableHighlight", 1);
					gizmoMaterial.Draw(Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX - 1);
					gizmoMaterial.BindProperty("enableHighlight", 0);
					if (Manipulators.hoveredGizmoId is not Gizmo.ScaleZ)
						gizmoMaterial.Draw((Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX)..(Gizmo.UniformScale - Gizmo.ViewCubeUpperLeftCornerMinusX - 2));
					screenGizmoMaterial.Draw();
				}
				else if (Manipulators.hoveredGizmoId is not Gizmo.RotateScreen)
				{
					gizmoMaterial.Draw(..9);
					gizmoMaterial.BindProperty("enableHighlight", 1);
					gizmoMaterial.Draw(9..);
					gizmoMaterial.BindProperty("enableHighlight", 0);
					screenGizmoMaterial.Draw();
				}
				else
				{
					gizmoMaterial.Draw();
					screenGizmoMaterial.BindProperty("enableHighlight", 1);
					screenGizmoMaterial.Draw();
					screenGizmoMaterial.BindProperty("enableHighlight", 0);
				}
			}
			else
			{
				gizmoMaterial.Draw();
				screenGizmoMaterial.Draw();
			}
		}
	}
}