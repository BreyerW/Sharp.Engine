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
			var s = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoHighlightPassShader.shader");
			gizmoMaterial.BindShader(1, s);

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
			gizmoMaterial.Draw();
		}
	}
}