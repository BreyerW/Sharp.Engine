using System;
using System.Numerics;
using Sharp;

//using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpAsset;
using SharpAsset.Pipeline;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace SharpSL.BackendRenderers.OpenGL
{
	public class EditorOpenGLRenderer : IEditorBackendRenderer
	{
		public void UnloadMatrix()
		{
			GL.PopMatrix();
		}

		public void LoadMatrix(ref Matrix4x4 mat)
		{
			GL.LoadMatrix(ref mat.M11);
			GL.PushMatrix();
		}

		public void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, ref float unColor, float width = 1f)
		{
			GL.Color4(ref unColor);
			GL.LineWidth(width);
			// GL.Enable(EnableCap.Blend);
			//GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Begin(PrimitiveType.Lines);
			GL.Vertex3(v1x, v1y, v1z);
			GL.Vertex3(v2x, v2y, v2z);
			//GL.Disable(EnableCap.Blend);
			GL.End();
			GL.LineWidth(1);
		}

		public void DrawSlicedQuad(float x1, float y1, float x2, float y2, float left, float right, float top, float bottom, ref float unColor)
		{
			GL.Color4(ref unColor);
			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(left, top);
			GL.Vertex3(x1, y1, 0);

			GL.TexCoord2(left, bottom);
			GL.Vertex3(x1, y2, 0);

			GL.TexCoord2(right, bottom);
			GL.Vertex3(x2, y2, 0);

			GL.TexCoord2(right, top);
			GL.Vertex3(x2, y1, 0);
			GL.End();
			GL.Color4((byte)0, (byte)0, (byte)0, (byte)255);

			/*
            *+---+----------------------+---+
            *| 3 |          4           | 7 |
            *+---+----------------------+---+
            *|   |                      |   |
            *| 2 |          5           | 8 |
            *|   |                      |   |
            *+---+----------------------+---+
            *| 1 |          6           | 9 |
            *+---+----------------------+---+
            */


		}

		public void DrawTexturedQuad(float x1, float y1, float x2, float y2, ref float unColor)
		{
			GL.UseProgram(3);
			GL.Color4(ref unColor);

			GL.Begin(BeginMode.Quads);

			GL.TexCoord2(0, 0);
			GL.Vertex3(x1, y1, 0);

			GL.TexCoord2(0, 1);
			GL.Vertex3(x1, y2, 0);

			GL.TexCoord2(1, 1);
			GL.Vertex3(x2, y2, 0);

			GL.TexCoord2(1, 0);
			GL.Vertex3(x2, y1, 0);

			GL.End();
			GL.Color4((byte)0, (byte)0, (byte)0, (byte)255);
		}

		public void DrawQuad(float x1, float y1, float x2, float y2, ref float unColor)
		{
			GL.Color4(ref unColor);
			GL.Begin(BeginMode.Quads);

			GL.Vertex3(x1, y1, 0);

			GL.Vertex3(x1, y2, 0);

			GL.Vertex3(x2, y2, 0);

			GL.Vertex3(x2, y1, 0);
			GL.Color4((byte)255, (byte)255, (byte)255, (byte)255);
			GL.End();
		}

		public void DrawLine(Vector3 v1, Vector3 v2, ref float color)
		{
			GL.Enable(EnableCap.Blend);

			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);//one|dstcolor, oneminusscrcolor|dstcolor,srcalpha|dstcolor,oneminussrcalpha|dstcolor
			DrawLine(v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, ref color);
		}

		public void DrawFilledPolyline(float size, float lineWidth, ref float color, ref Matrix4x4 mat, ref Vector3[] vecArray, bool fan = true)
		{
			//TurnOnDebugging();

			GL.Enable(EnableCap.Blend);

			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.LoadIdentity();
			GL.LoadMatrix(ref mat.M11);
			GL.PushMatrix();
			GL.Color4(ref color);
			GL.Begin(fan ? PrimitiveType.TriangleFan : PrimitiveType.Triangles);

			foreach (var vec in vecArray)
			{
				GL.Vertex3(vec.X, vec.Y, vec.Z);
			}

			GL.End();
			GL.PopMatrix();
		}

		private static GCHandle _logCallbackHandle;

		private static void ReceiveMessage(DebugSource debugSource, DebugType type, int id, DebugSeverity severity, int len,
			IntPtr msgPtr, IntPtr customObj)
		{
			var msg = Marshal.PtrToStringAnsi(msgPtr, len);
			Console.WriteLine("Source {0}; Type {1}; id {2}; Severity {3}; msg: '{4}'", debugSource, type, id, severity, msg);
		}

		private static readonly DebugProc debugDelegate = new DebugProc(ReceiveMessage);

		private void TurnOnDebugging()
		{
			GL.Enable(EnableCap.DebugOutput);
			GL.Enable(EnableCap.DebugOutputSynchronous);
			GCHandle.Alloc(debugDelegate);
			var nullptr = new IntPtr(0);
			GL.DebugMessageCallback(debugDelegate, nullptr);
		}

		public void DrawGrid(ref float color, Vector3 pos, float X, float Y, ref Matrix4x4 projMat, int cell_size = 16, int grid_size = 2560)
		{
			GL.UseProgram(0);
			int num = (int)Math.Round((double)(pos.X / (float)cell_size)) * cell_size;
			int num2 = (int)Math.Round((double)(pos.Y / (float)cell_size)) * cell_size;
			int num3 = grid_size / cell_size;
			GL.Disable(EnableCap.Texture2D);
			GL.LoadMatrix(ref projMat.M11);
			GL.PushMatrix();
			GL.Translate((float)num - (float)grid_size / 2f, 0f, (float)num2 - (float)grid_size / 2f);

			GL.Begin(PrimitiveType.Lines);
			GL.Color4(ref color);
			int num5;
			for (int i = 0; i < num3 + 1; i = num5 + 1)
			{
				int num4 = i * cell_size;
				GL.Vertex3(num4, 0, 0);
				GL.Vertex3(num4, 0, grid_size);
				GL.Vertex3(0, 0, num4);
				GL.Vertex3(grid_size, 0, num4);
				num5 = i;
			}
			GL.Enable(EnableCap.Texture2D);
			GL.Color4(0, 0, 0, 0);
			GL.End();
			GL.PopMatrix();
		}

		private void bvh_to_vertices(Bone joint, ref List<Vector4> vertices,
			ref List<ushort> indices, ref List<Matrix4x4> matrices,
			ushort parentIndex = 0)
		{
			// vertex from current joint is in 4-th ROW (column-major ordering)
			Matrix4x4.Invert(joint.Offset, out var inverted);
			var translatedVertex = new Vector4(inverted.M14, inverted.M24, inverted.M34, inverted.M44);//check column if wrong
			matrices.Add(inverted);
			//Console.WriteLine(translatedVertex.W);
			// pushing current
			vertices.Add(translatedVertex);
			// avoid putting root twice
			ushort myindex = (ushort)(vertices.Count - 1);
			if (parentIndex != myindex)
			{
				indices.Add(parentIndex);
				indices.Add(myindex);
			}

			// foreach child same thing
			foreach (var child in joint.Children)
				bvh_to_vertices(child, ref vertices, ref indices, ref matrices, myindex);
		}

		public void update(ref Skeleton skele)
		{
			List<Vector4> vertices = new List<Vector4>();
			List<ushort> bvhindices = new List<ushort>();
			List<Matrix4x4> matrices = new List<Matrix4x4>();

			bvh_to_vertices(skele.bones[0], ref vertices, ref bvhindices, ref matrices);

			GL.BindBuffer(BufferTarget.ArrayBuffer, skele.VBOV);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Marshal.SizeOf<Vector4>() * vertices.Count), vertices.ToArray(), BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
		}

		public void newinit(ref Skeleton skele)
		{
			GL.Enable(EnableCap.DepthTest);

			List<Vector4> vertices = new List<Vector4>();
			List<Matrix4x4> matriceses = new List<Matrix4x4>();
			List<ushort> bvhindices = new List<ushort>();

			bvh_to_vertices(skele.bones[0], ref vertices, ref bvhindices, ref matriceses);
			var bvh_elements = bvhindices.Count;

			GL.GenBuffers(1, out skele.VBOV);
			GL.BindBuffer(BufferTarget.ArrayBuffer, skele.VBOV);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Marshal.SizeOf<Vector4>() * vertices.Count), vertices.ToArray(), BufferUsageHint.DynamicDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.GenBuffers(1, out skele.VBOI);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, skele.VBOI);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(Marshal.SizeOf<ushort>() * bvhindices.Count), bvhindices.ToArray(), BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			//int sLocPosition = shader1.attribute("position");

			GL.GenVertexArrays(1, out skele.VAO);
			GL.BindVertexArray(skele.VAO);

			GL.BindBuffer(BufferTarget.ArrayBuffer, skele.VBOV);
			GL.EnableVertexAttribArray(0);
			GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, skele.VBOI);

			GL.BindVertexArray(0);
		}

		public void display(ref Skeleton skele)
		{
			//GL.ClearColor(0.5f, 0.5f, 0.5f, 1.0f);
			//GL.ClearDepth(1.0);
			//GL.Clear(ClearBufferMask.ColorBufferBit | Clea);

			//shader1.use();

			List<Vector4> vertices = new List<Vector4>();
			List<Matrix4x4> matrices = new List<Matrix4x4>();
			List<ushort> bvhindices = new List<ushort>();

			bvh_to_vertices(skele.bones[0], ref vertices, ref bvhindices, ref matrices);
			var bvh_elements = bvhindices.Count;
			var mats = matrices.ToArray();
			//GL.Enable (EnableCap.Light0);
			//GL.light
			//GL.LoadMatrix(ref skele.MVP);
			//GL.ShadeModel (ShadingModel.Flat);
			GL.LoadMatrix(ref skele.MVP.M11);
			//foreach (var childBone in skele.bones[0].Children)
			DisplayOcta(skele.bones[0]);
			var skeleShader = Pipeline.Get<Shader>().GetAsset("SkeletonShader");
			GL.UseProgram(skeleShader.Program);
			GL.UniformMatrix4(GL.GetUniformLocation(skeleShader.Program, "mvp_matrix"), 1, false, ref skele.MVP.M11);

			update(ref skele);
			GL.BindVertexArray(skele.VAO);

			GL.PointSize(5);
			GL.DrawElements(PrimitiveType.Lines, bvh_elements, DrawElementsType.UnsignedShort, (IntPtr)0);
			GL.LineWidth(3);
			GL.DrawElements(PrimitiveType.Points, bvh_elements, DrawElementsType.UnsignedShort, (IntPtr)0);
			GL.PointSize(1);
			GL.LineWidth(1);

			GL.BindVertexArray(0);
		}

		private void DisplayOcta(Bone bone)
		{
			Matrix4x4.Invert(bone.Offset, out var inverted);
			var mat = inverted;
			var head = new Vector3(inverted.M14, inverted.M24, inverted.M34);
			Matrix4x4.Invert(bone.Children[0].Offset, out var invertedChild);
			var boneVec = bone.Children.Count > 0 ? new Vector4(invertedChild.M14, invertedChild.M24, invertedChild.M34, invertedChild.M44) : Vector4.Transform(Vector4.Zero, mat);
			var tail = new Vector3(boneVec.X, boneVec.Y, boneVec.Z); //or borrow last length
																	 //Console.WriteLine ((copyMat-final).Length);
			draw_bone_solid_octahedral(ref mat, (head - tail).Length());
			foreach (var childBone in bone.Children)
				DisplayOcta(childBone);
		}

		private static float[][] bone_octahedral_verts = new float[6][] {
	new []{ 0.0f, 0.0f,  0.0f},
	new []{ 0.1f, 0.1f,  0.1f},
	new []{ 0.1f, 0.1f, -0.1f},
	new []{-0.1f, 0.1f, -0.1f},
	new []{-0.1f, 0.1f,  0.1f},
	new []{ 0.0f, 1.0f,  0.0f}
		};

		private static int[] bone_octahedral_wire_sides = new int[] { 0, 1, 5, 3, 0, 4, 5, 2 };
		private static int[] bone_octahedral_wire_square = new int[] { 1, 2, 3, 4, 1 };

		private static int[][] bone_octahedral_solid_tris = new int[8][] {
	new []{2, 1, 0}, /* bottom */
	new []{3, 2, 0},
	new []{4, 3, 0},
	new []{1, 4, 0},

	new []{5, 1, 2}, /* top */
	new []{5, 2, 3},
	new []{5, 3, 4},
	new []{5, 4, 1}
		};

		private static float M_SQRT1_2 = 0.707106781186547524401f;
		/* aligned with bone_octahedral_solid_tris */

		private static float[][] bone_octahedral_solid_normals = new float[8][] {
	new []{ M_SQRT1_2,   -M_SQRT1_2,    0.00000000f},
	new []{-0.00000000f, -M_SQRT1_2,   -M_SQRT1_2},
	new []{-M_SQRT1_2,   -M_SQRT1_2,    0.00000000f},
	new []{ 0.00000000f, -M_SQRT1_2,    M_SQRT1_2},
	new []{ 0.99388373f,  0.11043154f, -0.00000000f},
	new []{ 0.00000000f,  0.11043154f, -0.99388373f},
	new []{-0.99388373f,  0.11043154f,  0.00000000f},
	new []{ 0.00000000f,  0.11043154f,  0.99388373f}
		};

		private static void draw_bone_solid_octahedral(ref Matrix4x4 mat, float length)
		{
			//Console.WriteLine ("buuu");
			//	displist = GL.GenLists(1);
			//GL.NewList(displist,ListMode.Compile);

			GL.PushMatrix();

			GL.MultTransposeMatrix(ref mat.M11);
			GL.Scale(length, length, length);
			GL.Begin(PrimitiveType.Triangles);
			for (var i = 0; i < 8; i++)
			{
				GL.Normal3(bone_octahedral_solid_normals[i]);
				GL.Vertex3(bone_octahedral_verts[bone_octahedral_solid_tris[i][0]]);
				GL.Vertex3(bone_octahedral_verts[bone_octahedral_solid_tris[i][1]]);
				GL.Vertex3(bone_octahedral_verts[bone_octahedral_solid_tris[i][2]]);
			}

			GL.End();
			GL.PopMatrix();
			//GL.EndList();

			//GL.CallList(displist);
		}
	}
}