using System;
using System.Numerics;
using SharpAsset;
using SharpAsset.Pipeline;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		private static Shader shader;
		internal static Material gridLineMaterial;
		internal static Material gizmoMaterial;

		public static float gridSize = 256;
		public static float cellSize = 16;


		static DrawHelper()
		{
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");

			ref var circleMesh = ref Pipeline.Get<Mesh>().GetAsset("gizmo");
			FillMaterial(out gizmoMaterial, shader, circleMesh, Manipulators.xColor);
			gizmoMaterial.BindProperty("highlightColor", new Color(0.75f, 0.75f, 0.75f, 0.75f));
			gizmoMaterial.BindProperty("enableHighlight", 0);

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

		public static void DrawGizmo(in Matrix4x4 rotAndScaleMat)
		{
			gizmoMaterial.BindProperty("model", rotAndScaleMat);
			if (Manipulators.hoveredGizmoId is >= Gizmo.TranslateX)
			{
				if (Manipulators.useUniformScale is false)
				{
					if (Manipulators.hoveredGizmoId is not Gizmo.TranslateX)
						gizmoMaterial.Draw(..(Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX - 1));

					gizmoMaterial.BindProperty("enableHighlight", 1);
					gizmoMaterial.Draw(Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX - 1);
					gizmoMaterial.BindProperty("enableHighlight", 0);

					if (Manipulators.hoveredGizmoId is not Gizmo.ScaleZ)
						gizmoMaterial.Draw((Manipulators.hoveredGizmoId - Gizmo.ViewCubeUpperLeftCornerMinusX)..(Gizmo.UniformScale - Gizmo.ViewCubeUpperLeftCornerMinusX - 1));
				}
				else
				{
					gizmoMaterial.Draw(..9);
					gizmoMaterial.BindProperty("enableHighlight", 1);
					gizmoMaterial.Draw(9..);
					gizmoMaterial.BindProperty("enableHighlight", 0);
				}
			}
			else
				gizmoMaterial.Draw();
		}
	}
}