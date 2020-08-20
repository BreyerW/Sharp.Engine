using System;
using Sharp.Editor.Views;
using System.Numerics;
using SharpSL;
using SharpAsset;
using SharpAsset.Pipeline;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		private static Shader shader;
		private static Material planeMaterial;
		private static Material lineMaterial;
		private static Material coneMaterial;
		private static Material cubeMaterial;
		private static Material circleMaterial;
		/*private static readonly Color selectedColor = new Color(0xFF1080FF);
		private static readonly Color fillColor = new Color(0x801080FF);
		private static readonly Color xColor = new Color(0xFF0000AA);
		private static readonly Color yColor = new Color(0xFF00AA00);
		private static readonly Color zColor = new Color(0xFFAA0000);*/

		static DrawHelper()
		{
			shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\GizmoShader.shader");
			ref var mesh = ref Pipeline.Get<Mesh>().GetAsset("square");

			FillMaterial(out planeMaterial, ref mesh, ref shader);
			planeMaterial.BindProperty("len", new Vector2(7));

			ref var coneMesh = ref Pipeline.Get<Mesh>().GetAsset("cone");
			FillMaterial(out coneMaterial, ref coneMesh, ref shader);
			coneMaterial.BindProperty("len", new Vector2(5, 4));

			ref var cubeMesh = ref Pipeline.Get<Mesh>().GetAsset("cube");
			FillMaterial(out cubeMaterial, ref cubeMesh, ref shader);
			cubeMaterial.BindProperty("len", new Vector2(4));

			ref var circleMesh = ref Pipeline.Get<Mesh>().GetAsset("torus");
			FillMaterial(out circleMaterial, ref circleMesh, ref shader);
			circleMaterial.BindProperty("len", new Vector2(20));

			ref var lineMesh = ref Pipeline.Get<Mesh>().GetAsset("line");
			var newShader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\SingleSegmentLineShader.shader");
			FillMaterial(out lineMaterial, ref lineMesh, ref newShader);
			lineMaterial.BindProperty("width", 3f);
			lineMaterial.BindProperty("len", 15);
		}
		private static void FillMaterial(out Material mat, ref Mesh m, ref Shader s)
		{
			mat = new Material();
			mat.Shader = s;
			mat.BindProperty("mesh", m);
		}
		public static void DrawGrid(Color color, Vector3 pos, float X, float Y, ref Matrix4x4 projMat, int cell_size = 16, int grid_size = 2560)
		{
			MainEditorView.editorBackendRenderer.DrawGrid(ref color.r, pos, X, Y, ref projMat, cell_size, grid_size);
		}

		public static void DrawTranslationGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat, Color xColor, Color yColor, Color zColor)
		{
			var planeTranslation = Matrix4x4.CreateTranslation(2.5f, 2.5f, 0);
			planeMaterial.BindProperty("model", planeTranslation * yRotAndScaleMat);
			planeMaterial.BindProperty("color", xColor);
			planeMaterial.SendData();

			planeMaterial.BindProperty("model", planeTranslation * xRotAndScaleMat);
			planeMaterial.BindProperty("color", yColor);
			planeMaterial.SendData();
			var antiRotationZ = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, -NumericsExtensions.Deg2Rad * 90);
			planeMaterial.BindProperty("model", planeTranslation *antiRotationZ* zRotAndScaleMat);
			planeMaterial.BindProperty("color", zColor);
			planeMaterial.SendData();

			var coneTranslation = Matrix4x4.CreateTranslation(15, 0, 0);
			coneMaterial.BindProperty("model", coneTranslation * xRotAndScaleMat);
			coneMaterial.BindProperty("color", xColor);
			coneMaterial.SendData();
			coneMaterial.BindProperty("model", coneTranslation * zRotAndScaleMat);
			coneMaterial.BindProperty("color", yColor);
			coneMaterial.SendData();
			coneMaterial.BindProperty("model", coneTranslation * yRotAndScaleMat);
			coneMaterial.BindProperty("color", zColor);
			coneMaterial.SendData();


			//GL.Enable(EnableCap.LineSmooth);
			//GL.Enable(EnableCap.Blend);
			//GL.DepthMask(false);
			lineMaterial.BindProperty("model", xRotAndScaleMat);
			lineMaterial.BindProperty("color", xColor);
			lineMaterial.SendData();

			lineMaterial.BindProperty("model", zRotAndScaleMat);
			lineMaterial.BindProperty("color", yColor);
			lineMaterial.SendData();

			lineMaterial.BindProperty("model", yRotAndScaleMat);
			lineMaterial.BindProperty("color", zColor);
			lineMaterial.SendData();
			//GL.Disable(EnableCap.Blend);
			//GL.DepthMask(true);
		}

		public static void DrawRotationGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat, Color xColor, Color yColor, Color zColor)
		{
			circleMaterial.BindProperty("model", yRotAndScaleMat);
			circleMaterial.BindProperty("color", xColor);
			circleMaterial.SendData();

			circleMaterial.BindProperty("model", xRotAndScaleMat);
			circleMaterial.BindProperty("color", yColor);
			circleMaterial.SendData();

			circleMaterial.BindProperty("model",zRotAndScaleMat);
			circleMaterial.BindProperty("color", zColor);
			circleMaterial.SendData();
		}

		public static void DrawScaleGizmo(in Matrix4x4 xRotAndScaleMat, in Matrix4x4 yRotAndScaleMat, in Matrix4x4 zRotAndScaleMat, Color xColor, Color yColor, Color zColor, Vector3 offset = default(Vector3))
		{
			var cubeTranslation = Matrix4x4.CreateTranslation(20, -2, -2);
			cubeMaterial.BindProperty("model", cubeTranslation * xRotAndScaleMat);
			cubeMaterial.BindProperty("color", xColor);
			cubeMaterial.SendData();

			cubeMaterial.BindProperty("model", cubeTranslation * zRotAndScaleMat);
			cubeMaterial.BindProperty("color", yColor);
			cubeMaterial.SendData();

			cubeMaterial.BindProperty("model", cubeTranslation * yRotAndScaleMat);
			cubeMaterial.BindProperty("color", zColor);
			cubeMaterial.SendData();
		}

		public static void DrawFilledPolyline(float size, float lineWidth, Color color, ref Matrix4x4 mat, ref Vector3[] vecArray, bool fan = true)
		{
			MainEditorView.editorBackendRenderer.DrawFilledPolyline(size, lineWidth, ref color.r, ref mat, ref vecArray, fan);
		}
	}
}