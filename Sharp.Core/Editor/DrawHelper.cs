using System;
using System.Numerics;
using SharpAsset;
using SharpAsset.Pipeline;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		private static Shader shader;
		internal static Material planeMaterialXY;
		internal static Material planeMaterialYZ;
		internal static Material planeMaterialZX;
		internal static Material lineMaterialX;
		internal static Material lineMaterialY;
		internal static Material lineMaterialZ;
		internal static Material gridLineMaterial;
		internal static Material coneMaterialX;
		internal static Material coneMaterialY;
		internal static Material coneMaterialZ;
		internal static Material cubeMaterialX;
		internal static Material cubeMaterialY;
		internal static Material cubeMaterialZ;
		internal static Material circleMaterialX;
		internal static Material circleMaterialY;
		internal static Material circleMaterialZ;
		public static float gridSize = 256;
		public static float cellSize = 16;


		static DrawHelper()
		{
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");
			ref var mesh = ref Pipeline.Get<Mesh>().GetAsset("square");

			FillMaterial(out planeMaterialXY, shader, mesh, Manipulators.xColor);
			planeMaterialXY.BindProperty("len", new Vector2(7));
			FillMaterial(out planeMaterialYZ, shader, mesh, Manipulators.yColor);
			planeMaterialYZ.BindProperty("len", new Vector2(7));
			FillMaterial(out planeMaterialZX, shader, mesh, Manipulators.zColor);
			planeMaterialZX.BindProperty("len", new Vector2(7));


			ref var coneMesh = ref Pipeline.Get<Mesh>().GetAsset("cone");
			FillMaterial(out coneMaterialX, shader, coneMesh, Manipulators.xColor);
			coneMaterialX.BindProperty("len", new Vector2(5, 4));
			FillMaterial(out coneMaterialY, shader, coneMesh, Manipulators.yColor);
			coneMaterialY.BindProperty("len", new Vector2(5, 4));
			FillMaterial(out coneMaterialZ, shader, coneMesh, Manipulators.zColor);
			coneMaterialZ.BindProperty("len", new Vector2(5, 4));

			ref var cubeMesh = ref Pipeline.Get<Mesh>().GetAsset("cube");
			FillMaterial(out cubeMaterialX, shader, cubeMesh, Manipulators.xColor);
			cubeMaterialX.BindProperty("len", new Vector2(4));
			FillMaterial(out cubeMaterialY, shader, cubeMesh, Manipulators.yColor);
			cubeMaterialY.BindProperty("len", new Vector2(4));
			FillMaterial(out cubeMaterialZ, shader, cubeMesh, Manipulators.zColor);
			cubeMaterialZ.BindProperty("len", new Vector2(4));

			ref var circleMesh = ref Pipeline.Get<Mesh>().GetAsset("torus");
			FillMaterial(out circleMaterialX, shader, circleMesh, Manipulators.xColor);
			circleMaterialX.BindProperty("len", new Vector2(20));
			FillMaterial(out circleMaterialY, shader, circleMesh, Manipulators.yColor);
			circleMaterialY.BindProperty("len", new Vector2(20));
			FillMaterial(out circleMaterialZ, shader, circleMesh, Manipulators.zColor);
			circleMaterialZ.BindProperty("len", new Vector2(20));

			ref var lineMesh = ref Pipeline.Get<Mesh>().GetAsset("line");
			var newShader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\SingleSegmentLineShader.shader");
			FillMaterial(out lineMaterialX, newShader, lineMesh, Manipulators.xColor);
			lineMaterialX.BindProperty("width", 3f);
			lineMaterialX.BindProperty("len", 15);
			FillMaterial(out lineMaterialY, newShader, lineMesh, Manipulators.yColor);
			lineMaterialY.BindProperty("width", 3f);
			lineMaterialY.BindProperty("len", 15);
			FillMaterial(out lineMaterialZ, newShader, lineMesh, Manipulators.zColor);
			lineMaterialZ.BindProperty("width", 3f);
			lineMaterialZ.BindProperty("len", 15);
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

		public static void DrawTranslationGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat)
		{
			var planeTranslation = Matrix4x4.CreateTranslation(2.5f, 2.5f, 0);
			var rotationX = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, NumericsExtensions.Deg2Rad * 180);
			planeMaterialZX.BindProperty("model", planeTranslation * xRotAndScaleMat);
			planeMaterialZX.Draw();

			planeMaterialYZ.BindProperty("model", planeTranslation * rotationX * yRotAndScaleMat);
			planeMaterialYZ.Draw();

			var antiRotationZ = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, -NumericsExtensions.Deg2Rad * 90);
			planeMaterialXY.BindProperty("model", planeTranslation * antiRotationZ * zRotAndScaleMat);
			planeMaterialXY.Draw();

			var rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, NumericsExtensions.Deg2Rad * 180);
			var coneTranslation = Matrix4x4.CreateTranslation(15, 0, 0);
			coneMaterialX.BindProperty("model", coneTranslation * xRotAndScaleMat);
			coneMaterialX.Draw();

			coneMaterialY.BindProperty("model", coneTranslation * zRotAndScaleMat);
			coneMaterialY.Draw();

			coneMaterialZ.BindProperty("model", coneTranslation * rotation * yRotAndScaleMat);
			coneMaterialZ.Draw();

			lineMaterialX.BindProperty("model", xRotAndScaleMat);
			lineMaterialX.Draw();

			lineMaterialY.BindProperty("model", zRotAndScaleMat);
			lineMaterialY.Draw();

			lineMaterialZ.BindProperty("model", rotation * yRotAndScaleMat);
			lineMaterialZ.Draw();
		}

		public static void DrawRotationGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat)
		{
			circleMaterialX.BindProperty("model", yRotAndScaleMat);
			circleMaterialX.Draw();

			circleMaterialY.BindProperty("model", xRotAndScaleMat);
			circleMaterialY.Draw();

			circleMaterialZ.BindProperty("model", zRotAndScaleMat);
			circleMaterialZ.Draw();
		}

		public static void DrawScaleGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat, Vector3 offset, Gizmo selectedGizmo)
		{
			var offsetTranslation = Matrix4x4.CreateTranslation(offset);
			var cubeTranslation = Matrix4x4.CreateTranslation(20, -2, -2);
			cubeMaterialX.BindProperty("model", cubeTranslation * xRotAndScaleMat * (selectedGizmo is Gizmo.ScaleX ? offsetTranslation : Matrix4x4.Identity));
			cubeMaterialX.Draw();

			cubeMaterialY.BindProperty("model", cubeTranslation * zRotAndScaleMat * (selectedGizmo is Gizmo.ScaleY ? offsetTranslation : Matrix4x4.Identity));
			cubeMaterialY.Draw();
			var rotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, NumericsExtensions.Deg2Rad * 180);
			cubeMaterialZ.BindProperty("model", cubeTranslation * rotation * yRotAndScaleMat * (selectedGizmo is Gizmo.ScaleZ ? offsetTranslation : Matrix4x4.Identity));
			cubeMaterialZ.Draw();
		}
	}
}