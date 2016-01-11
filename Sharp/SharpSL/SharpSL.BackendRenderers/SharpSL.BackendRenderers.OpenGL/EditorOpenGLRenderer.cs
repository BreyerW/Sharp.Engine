using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Drawing;

namespace SharpSL.BackendRenderers.OpenGL
{
	public class EditorOpenGLRenderer:IEditorBackendRenderer
	{
		public void DrawRotateGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor)
		{
			//this.SetupGraphic();
			GL.LineWidth(thickness);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.DepthFunc(DepthFunction.Always);
			this.DrawCircleY(3f * scale, 3f, yColor);
			this.DrawCircleZ(3f * scale, 3f, zColor);
			this.DrawCircleX(3f * scale, 3f, xColor);
			GL.LineWidth(1f);
			GL.DepthFunc(DepthFunction.Less);
		}
		public void DrawTranslateGizmo(float thickness, float scale, Color xColor, Color yColor, Color zColor)
		{
			GL.LineWidth(thickness);
			GL.Enable(EnableCap.Blend);
			GL.Disable(EnableCap.DepthTest);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			this.DrawLine(0f, 0f, 0f, 0f, 3f * scale, 0f, yColor);
			this.DrawConeY(0.2f * scale, 0.5f * scale, 3f * scale);
			this.DrawPlaneXZ(1f * scale, 0.3f * scale, yColor);
			this.DrawLine(0f, 0f, 0f, 3f * scale, 0f, 0f, xColor);
			this.DrawConeX(0.2f * scale, 0.5f * scale, 3f * scale);
			this.DrawPlaneZY(1f * scale, 0.3f * scale, xColor);
			this.DrawLine(0f, 0f, 0f, 0f, 0f, 3f * scale, zColor);
			this.DrawConeZ(0.2f * scale, 0.5f * scale, 3f * scale);
			this.DrawPlaneYX(1f * scale, 0.3f * scale, zColor);
			GL.Color4(Color.White);
			GL.LineWidth(1f);
			GL.Enable(EnableCap.DepthTest);
		}
		public void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor)
		{
			GL.Color4(unColor);
			GL.Enable(EnableCap.Blend);
			GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
			GL.Begin(PrimitiveType.Lines);
			GL.Vertex3(v1x, v1y, v1z);
			GL.Vertex3(v2x, v2y, v2z);
			GL.Disable(EnableCap.Blend);
			GL.End();
		}
		public void DrawSelectionSquare(float x1, float y1, float x2, float y2, Color unColor)
		{
			GL.Color4(unColor);
			GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
			GL.Rect(x1, y1, x2, y2);
		}
		public void DrawCircleX(float size, float lineWidth, Color unColor)
		{
			GL.Color4(unColor);
			GL.Begin(BeginMode.LineLoop);
			float num = 0.03141593f;
			for (float num2 = 0f; num2 < 6.28318548f; num2 += num)
			{
				GL.Vertex3(0.0, Math.Sin((double)num2) * (double)size, Math.Cos((double)num2) * (double)size);
			}
			GL.End();
		}
		public void DrawCircleY(float size, float lineWidth, Color unColor)
		{
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.LineLoop);
			float num = 0.03141593f;
			for (float num2 = 0f; num2 < 6.28318548f; num2 += num)
			{
				GL.Vertex3(Math.Sin((double)num2) * (double)size, 0.0, Math.Cos((double)num2) * (double)size);
			}
			GL.End();
		}
		public void DrawCircleZ(float size, float lineWidth, Color unColor)
		{
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.LineLoop);
			float num = 0.03141593f;
			for (float num2 = 0f; num2 < 6.28318548f; num2 += num)
			{
				GL.Vertex3(Math.Sin((double)num2) * (double)size, Math.Cos((double)num2) * (double)size, 0.0);
			}
			GL.End();
		}
		public void DrawSphere(float radius, int lats, int longs, Color unColor)
		{
			GL.Color4(unColor);
			int num10;
			for (int i = 1; i <= lats; i = num10 + 1)
			{
				float num = 3.14159274f * (-0.5f + (float)(i - 1) / (float)lats);
				float num2 = radius * (float)Math.Sin((double)num);
				float num3 = radius * (float)Math.Cos((double)num);
				float num4 = 3.14159274f * (-0.5f + (float)i / (float)lats);
				float num5 = radius * (float)Math.Sin((double)num4);
				float num6 = radius * (float)Math.Cos((double)num4);
				GL.Begin(PrimitiveType.QuadStrip);
				for (int j = 0; j <= longs; j = num10 + 1)
				{
					float num7 = 6.28318548f * (float)(j - 1) / (float)longs;
					float num8 = (float)Math.Cos((double)num7);
					float num9 = (float)Math.Sin((double)num7);
					GL.Normal3(num8 * num6, num9 * num6, num5);
					GL.Vertex3(num8 * num6, num9 * num6, num5);
					GL.Normal3(num8 * num3, num9 * num3, num2);
					GL.Vertex3(num8 * num3, num9 * num3, num2);
					num10 = j;
				}
				GL.End();
				num10 = i;
			}
		}
		public void DrawCube(float size, float posX, float posY, float posZ, Color unColor)
		{
			uint[] indices = new uint[]
			{
				0u,
				1u,
				2u,
				3u,
				2u,
				1u,
				4u,
				0u,
				6u,
				6u,
				0u,
				2u,
				5u,
				1u,
				4u,
				4u,
				1u,
				0u,
				7u,
				3u,
				1u,
				7u,
				1u,
				5u,
				5u,
				4u,
				7u,
				7u,
				4u,
				6u,
				7u,
				2u,
				3u,
				7u,
				6u,
				2u
			};
			float[] pointer = new float[]
			{
				1f,
				1f,
				1f,
				-1f,
				1f,
				1f,
				1f,
				-1f,
				1f,
				-1f,
				-1f,
				1f,
				1f,
				1f,
				-1f,
				-1f,
				1f,
				-1f,
				1f,
				-1f,
				-1f,
				-1f,
				-1f,
				-1f
			};
			size *= 0.5f;
			GL.PushMatrix();
			GL.Translate(posX, posY, posZ);
			GL.Scale(size, size, size);
			GL.Color4(unColor);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.VertexPointer<float>(3, VertexPointerType.Float, 0, pointer);
			GL.DrawElements<uint>(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, indices);
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.PopMatrix();
		}
		public void DrawPlaneXZ(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(sizeOffset, 0f, sizeOffset);
			GL.Vertex3(size, 0f, sizeOffset);
			GL.Vertex3(size, 0f, size);
			GL.Vertex3(sizeOffset, 0f, size);
			GL.End();
		}
		public void DrawPlaneZY(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(0f, sizeOffset, sizeOffset);
			GL.Vertex3(0f, size, sizeOffset);
			GL.Vertex3(0f, size, size);
			GL.Vertex3(0f, sizeOffset, size);
			GL.End();
		}
		public void DrawPlaneYX(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(sizeOffset, sizeOffset, 0f);
			GL.Vertex3(sizeOffset, size, 0f);
			GL.Vertex3(size, size, 0f);
			GL.Vertex3(size, sizeOffset, 0f);
			GL.End();
		}
		public void DrawConeX(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(offset + height, 0f, 0f);
			float z;
			for (float num = 0f; num < 6.28318548f; num += 0.1f)
			{
				float y = (float)Math.Cos((double)num) * width;
				z = (float)Math.Sin((double)num) * width;
				GL.Vertex3(offset, y, z);
			}
			z = 0f;
			GL.Vertex3(offset, width, z);
			GL.End();
		}
		public void DrawConeZ(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(0f, 0f, offset + height);
			float y;
			for (float num = 0f; num < 6.28318548f; num += 0.1f)
			{
				float x = (float)Math.Cos((double)num) * width;
				y = (float)Math.Sin((double)num) * width;
				GL.Vertex3(x, y, offset);
			}
			y = 0f;
			GL.Vertex3(width, y, offset);
			GL.End();
		}
		public void DrawConeY(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(0f, offset + height, 0f);
			float z;
			for (float num = 0f; num < 6.28318548f; num += 0.1f)
			{
				float x = (float)Math.Cos((double)num) * width;
				z = (float)Math.Sin((double)num) * width;
				GL.Vertex3(x, offset, z);
			}
			z = 0f;
			GL.Vertex3(width, offset, z);
			GL.End();
		}
		public void DrawCone(float width, float height, float offset, Vector3 axis)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			axis.X = ((axis.X != 1f) ? (offset + height) : 0f);
			axis.X = ((axis.Y == 1f) ? (offset + height) : 0f);
			axis.X = ((axis.Z == 1f) ? (offset + height) : 0f);
			GL.Vertex3(axis);
			float z;
			for (float num = 0f; num < 6.28318548f; num += 0.1f)
			{
				float x = (float)Math.Cos((double)num) * width;
				z = (float)Math.Sin((double)num) * width;
				GL.Vertex3(x, offset, z);
			}
			z = 0f;
			GL.Vertex3(width, offset, z);
			GL.End();
		}
		public void DrawRectangle(Vector3 pos1, Vector3 pos2)
		{
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos2.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.End();
		}
		public void DrawBox(Vector3 pos1, Vector3 pos2)
		{
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y, pos2.Z);
			GL.End();
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos2.Z);
			GL.End();
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos2.Z);
			GL.End();
		}
		public void DrawGrid(Color color, Vector3 pos, float X, float Y, int cell_size = 16, int grid_size = 2560)
		{
			GL.UseProgram(0);
			int num = (int)Math.Round((double)(pos.X / (float)cell_size)) * cell_size;
			int num2 = (int)Math.Round((double)(pos.Y / (float)cell_size)) * cell_size;
			int num3 = grid_size / cell_size;
			GL.PushMatrix();
			GL.Translate((float)num - (float)grid_size / 2f, 0f, (float)num2 - (float)grid_size / 2f);
			GL.Color3(color);
			GL.Begin(PrimitiveType.Lines);
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
			GL.End();
			GL.PopMatrix();
		}
	}
}

