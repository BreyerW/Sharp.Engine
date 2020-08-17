using System;
using Sharp.Editor.Views;
using System.Numerics;
using SharpSL;
using Plane = SharpSL.Plane;
using SharpAsset;
using SharpAsset.Pipeline;
using OpenTK.Graphics.OpenGL;

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
		private static readonly Color selectedColor = new Color(0xFF1080FF);
		private static readonly Color fillColor = new Color(0x801080FF);
		private static readonly Color xColor = new Color(0xFF0000AA);
		private static readonly Color yColor = new Color(0xFF00AA00);
		private static readonly Color zColor = new Color(0xFFAA0000);

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
			MainEditorView.editorBackendRenderer.DrawPlaneXZ(size, sizeOffset, ref unColor.r);
		}

		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneZY(float size, float sizeOffset, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawPlaneZY(size, sizeOffset, ref unColor.r);
		}

		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneYX(float size, float sizeOffset, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawPlaneYX(size, sizeOffset, ref unColor.r);
		}

		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="lats"></param>
		/// <param name="longs"></param>
		public static void DrawSphere(float radius, int lats, int longs, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawSphere(radius, lats, longs, ref unColor.r);
		}

		public static void DrawCircle(float lineWidth, float size, Color unColor, Plane plane, float angle = 360, bool filled = false)
		{
			MainEditorView.editorBackendRenderer.DrawCircle(size, lineWidth, ref unColor.r, plane, angle, filled);
		}

		public static void DrawSelectionSquare(float x1, float y1, float x2, float y2, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawTexturedQuad(x1, y1, x2, y2, ref unColor.r);
		}

		public static void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor)
		{
			MainEditorView.editorBackendRenderer.DrawLine(v1x, v1y, v1z, v2x, v2y, v2z, ref unColor.r);
		}

		public static void DrawTranslationGizmo(Entity e, Vector2 winSize, float thickness, float scale, Color xColor, Color yColor, Color zColor)
		{
			Material.BindGlobalProperty("viewPort", winSize);
			MainEditorView.editorBackendRenderer.DrawTranslateGizmo(thickness, scale, ref xColor.r, ref yColor.r, ref zColor.r);
			Matrix4x4.Decompose(SceneView.globalMode ? e.transform.ModelMatrix.Inverted() : e.transform.ModelMatrix, out _, out var rot, out var trans);

			var scaleMat = Matrix4x4.CreateScale(scale, scale, scale);
			var mat = Matrix4x4.CreateFromQuaternion(rot) * Matrix4x4.CreateTranslation(trans);

			MainWindow.backendRenderer.Use(planeMaterial.Shader.Program);
			var planeTranslationAndScale = Matrix4x4.CreateTranslation(2.5f, 2.5f, 0) * scaleMat;
			var rotationX = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, NumericsExtensions.Deg2Rad * 90);
			var rotationY = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, -NumericsExtensions.Deg2Rad * 90);
			var rotationZ = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, NumericsExtensions.Deg2Rad * 90);

			planeMaterial.BindProperty("model", planeTranslationAndScale * rotationY * mat);
			planeMaterial.BindProperty("color", xColor);
			planeMaterial.SendData();

			planeMaterial.BindProperty("model", planeTranslationAndScale * rotationX * mat);
			planeMaterial.BindProperty("color", yColor);
			planeMaterial.SendData();
			planeMaterial.BindProperty("model", planeTranslationAndScale * mat);
			planeMaterial.BindProperty("color", zColor);
			planeMaterial.SendData();

			var coneTranslationAndScale = Matrix4x4.CreateTranslation(15, 0, 0) * scaleMat;
			coneMaterial.BindProperty("model", coneTranslationAndScale * mat);
			coneMaterial.BindProperty("color", xColor);
			coneMaterial.SendData();
			coneMaterial.BindProperty("model", coneTranslationAndScale * rotationZ * mat);
			coneMaterial.BindProperty("color", yColor);
			coneMaterial.SendData();
			coneMaterial.BindProperty("model", coneTranslationAndScale * rotationY * mat);
			coneMaterial.BindProperty("color", zColor);
			coneMaterial.SendData();

			var cubeTranslationAndScale = Matrix4x4.CreateTranslation(20, -2, -2) * scaleMat;
			cubeMaterial.BindProperty("model", cubeTranslationAndScale * mat);
			cubeMaterial.BindProperty("color", xColor);
			cubeMaterial.SendData();
			cubeMaterial.BindProperty("model", cubeTranslationAndScale * rotationZ * mat);
			cubeMaterial.BindProperty("color", yColor);
			cubeMaterial.SendData();
			cubeMaterial.BindProperty("model", cubeTranslationAndScale * rotationY * mat);
			cubeMaterial.BindProperty("color", zColor);
			cubeMaterial.SendData();
			//GL.Enable(EnableCap.LineSmooth);
			//GL.Enable(EnableCap.Blend);
			//GL.DepthMask(false);

			circleMaterial.BindProperty("model", scaleMat * rotationY * mat);
			circleMaterial.BindProperty("color", xColor);
			circleMaterial.SendData();

			circleMaterial.BindProperty("model", scaleMat * rotationX * mat);
			circleMaterial.BindProperty("color", yColor);
			circleMaterial.SendData();

			circleMaterial.BindProperty("model", scaleMat * rotationZ * mat);
			circleMaterial.BindProperty("color", zColor);
			circleMaterial.SendData();

			MainWindow.backendRenderer.Use(lineMaterial.Shader.Program);
			lineMaterial.BindProperty("model", scaleMat * mat);
			lineMaterial.BindProperty("color", xColor);
			lineMaterial.SendData();

			lineMaterial.BindProperty("model", scaleMat * rotationZ * mat);
			lineMaterial.BindProperty("color", yColor);
			lineMaterial.SendData();

			lineMaterial.BindProperty("model", scaleMat * rotationY * mat);
			lineMaterial.BindProperty("color", zColor);
			lineMaterial.SendData();


			MainWindow.backendRenderer.Use(0);
			//GL.Disable(EnableCap.Blend);
			//GL.DepthMask(true);
		}

		public static void DrawRotationGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor)
		{
			MainEditorView.editorBackendRenderer.DrawRotateGizmo(thickness, scale, ref xColor.r, ref yColor.r, ref zColor.r);
		}

		public static void DrawScaleGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor, Vector3 offset = default(Vector3))
		{
			MainEditorView.editorBackendRenderer.DrawScaleGizmo(thickness, scale, ref xColor.r, ref yColor.r, ref zColor.r, offset);
		}

		public static void DrawFilledPolyline(float size, float lineWidth, Color color, ref Matrix4x4 mat, ref Vector3[] vecArray, bool fan = true)
		{
			MainEditorView.editorBackendRenderer.DrawFilledPolyline(size, lineWidth, ref color.r, ref mat, ref vecArray, fan);
		}
	}
}