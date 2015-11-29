using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Drawing;

namespace Sharp.Editor
{
	public static class DrawHelper
	{
		public static void DrawGrid (System.Drawing.Color color,Vector3 pos, float X, float Y, int cell_size = 16, int grid_size = 2560)
		{
			GL.UseProgram (0);
			int dX = (int)Math.Round (pos.X / cell_size) * cell_size;
			int dZ = (int)Math.Round (pos.Y / cell_size) * cell_size;

			int ratio = grid_size / cell_size;
			GL.PushMatrix ();

			GL.Translate (dX - grid_size / 2f, 0, dZ - grid_size / 2f);

			int i;
			//GL.LineWidth (3);
			GL.Color3 (color);
			GL.Begin (PrimitiveType.Lines);

			for (i = 0; i < ratio + 1; i++) {
				int current = i * cell_size;

				GL.Vertex3 (current, 0, 0);
				GL.Vertex3 (current, 0, grid_size);

				GL.Vertex3 (0, 0, current);
				GL.Vertex3 (grid_size, 0, current);
			}
			GL.End ();

			GL.PopMatrix ();
		}
		public static void DrawBox(Vector3 pos1, Vector3 pos2)
		{
			//GL.UseProgram (0);
			// Bottom

			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y, pos2.Z);
			GL.End();

			// Top
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos2.Z);
			GL.End();

			// Vertical
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos1.Z);
			GL.Vertex3(pos2.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos2.X, pos2.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y, pos2.Z);
			GL.Vertex3(pos1.X, pos2.Y, pos2.Z);
			GL.End ();	
		}
		public static void DrawRectangle(Vector3 pos1, Vector3 pos2){
			
			GL.Begin(PrimitiveType.LineLoop);
			GL.Vertex3(pos1.X, pos1.Y,pos1.Z);
			GL.Vertex3(pos1.X, pos2.Y,pos1.Z);
			GL.Vertex3(pos2.X, pos2.Y,pos2.Z);
			GL.Vertex3(pos2.X, pos1.Y,pos2.Z);
			GL.Vertex3(pos1.X, pos1.Y,pos1.Z);
			GL.End ();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawCone(float width, float height, float offset,Vector3 axis)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			axis.X= axis.X!=1f? (offset + height): 0;
			axis.X= axis.Y==1f? (offset + height): 0;
			axis.X= axis.Z==1f? (offset + height): 0;
			GL.Vertex3(axis);
			float x, z;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += 0.1f)
			{
				x = ((float)Math.Cos((double)rads) * width);
				z = ((float)Math.Sin((double)rads) * width);
				GL.Vertex3(x, offset, z);
			}
			x = width;
			z = 0;
			GL.Vertex3(x, offset, z);
			GL.End();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeY(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(0f, offset + height, 0f);
			float x, z;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += 0.1f)
			{
				x = ((float)Math.Cos((double)rads) * width);
				z = ((float)Math.Sin((double)rads) * width);
				GL.Vertex3(x, offset, z);
			}
			x = width;
			z = 0;
			GL.Vertex3(x, offset, z);
			GL.End();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeZ(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(0f, 0f, offset + height);
			float x, z;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += 0.1f)
			{
				x = ((float)Math.Cos((double)rads) * width);
				z = ((float)Math.Sin((double)rads) * width);
				GL.Vertex3(x, z, offset);
			}
			x = width;
			z = 0;
			GL.Vertex3(x, z, offset);
			GL.End();
		}

		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="offset"></param>
		public static void DrawConeX(float width, float height, float offset)
		{
			GL.Begin(PrimitiveType.TriangleFan);
			GL.Vertex3(offset + height, 0f, 0f);
			float x, z;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += 0.1f)
			{
				x = ((float)Math.Cos((double)rads) * width);
				z = ((float)Math.Sin((double)rads) * width);
				GL.Vertex3(offset, x, z);
			}
			x = width;
			z = 0;
			GL.Vertex3(offset, x, z);
			GL.End();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneXZ(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(sizeOffset, 0, sizeOffset);
			GL.Vertex3(size, 0, sizeOffset);
			GL.Vertex3(size, 0, size);
			GL.Vertex3(sizeOffset, 0, size);
			GL.End();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneZY(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(0, sizeOffset, sizeOffset);
			GL.Vertex3(0, size, sizeOffset);
			GL.Vertex3(0, size, size);
			GL.Vertex3(0, sizeOffset, size);
			GL.End();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/
		/// </summary>
		/// <param name="size"></param>
		/// <param name="sizeOffset"></param>
		public static void DrawPlaneYX(float size, float sizeOffset, Color unColor)
		{
			size += sizeOffset;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.Quads);
			GL.Vertex3(sizeOffset, sizeOffset, 0);
			GL.Vertex3(sizeOffset, size, 0);
			GL.Vertex3(size, size, 0);
			GL.Vertex3(size, sizeOffset, 0);
			GL.End();
		}

		public static void DrawCube(float size, float posX, float posY, float posZ, Color unColor)
		{
			uint[] indices =
			{
				0, 1, 2,
				3, 2, 1,
				4, 0, 6,
				6, 0, 2,
				5, 1, 4,
				4, 1, 0,
				7, 3, 1,
				7, 1, 5,
				5, 4, 7,
				7, 4, 6,
				7, 2, 3,
				7, 6, 2
			};

			float[] vertices =
			{	
				1,  1,  1,
				-1,  1,  1,
				1, -1,  1,
				-1, -1,  1,
				1,  1, -1,
				-1,  1, -1,
				1, -1, -1,
				-1, -1, -1
			};

			size *= 0.5f;

			GL.PushMatrix();
			GL.Translate(posX, posY, posZ);
			GL.Scale(size, size, size);
			GL.Color4(unColor);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.VertexPointer(3, VertexPointerType.Float, 0, vertices);
			GL.DrawElements(PrimitiveType.Triangles, 36, DrawElementsType.UnsignedInt, indices);
			GL.DisableClientState(ArrayCap.VertexArray);

			GL.PopMatrix();
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/ 
		/// </summary>
		/// <param name="radius"></param>
		/// <param name="lats"></param>
		/// <param name="longs"></param>
		public static void DrawSphere(float radius, int lats, int longs, Color unColor)
		{
			GL.Color4(unColor);

			for (int i = 1; i <= lats; i++)
			{
				float lat0 = MathHelper.Pi * (-0.5f + (float)(i - 1) / (float)lats);
				float z0 = radius * (float)Math.Sin((float)lat0);
				float zr0 = radius * (float)Math.Cos((float)lat0);

				float lat1 = MathHelper.Pi * (-0.5f + (float)i / (float)lats);
				float z1 = radius * (float)Math.Sin((float)lat1);
				float zr1 = radius * (float)Math.Cos((float)lat1);

				GL.Begin(PrimitiveType.QuadStrip);
				for (int j = 0; j <= longs; j++)
				{
					float lng = 2 * MathHelper.Pi * (float)(j - 1) / (float)longs;
					float x = (float)Math.Cos((float)lng);
					float y = (float)Math.Sin((float)lng);
					GL.Normal3(x * zr1, y * zr1, z1);
					GL.Vertex3(x * zr1, y * zr1, z1);
					GL.Normal3(x * zr0, y * zr0, z0);
					GL.Vertex3(x * zr0, y * zr0, z0);
				}
				GL.End();
			}
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleY(float size, float lineWidth, Color unColor)
		{
			
			//this.LineWidth = lineWidth;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.LineLoop);
			float rads_step = MathHelper.Pi * 0.01f;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += rads_step)
				GL.Vertex3(Math.Sin((double)rads) * (double)size, 0.0f, Math.Cos((double)rads) * (double)size);
			GL.End();
			//this.LineWidth = 1.0f;
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleX(float size, float lineWidth, Color unColor)
		{
			//this.LineWidth = lineWidth;
			GL.Color4(unColor);
			GL.Begin(BeginMode.LineLoop);
			float rads_step = MathHelper.Pi * 0.01f;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += rads_step)
				GL.Vertex3(0.0f, Math.Sin((double)rads) * (double)size, Math.Cos((double)rads) * (double)size);
			GL.End();
			//this.LineWidth = 1.0f;
		}
		/// <summary>
		/// From http://code.google.com/p/3d-editor-toolkit/  
		/// </summary>
		/// <param name="size"></param>
		public static void DrawCircleZ(float size, float lineWidth, Color unColor)
		{
		//	this.LineWidth = lineWidth;
			GL.Color4(unColor);
			GL.Begin(PrimitiveType.LineLoop);
			float rads_step = MathHelper.Pi * 0.01f;
			for (float rads = 0.0f; rads < MathHelper.Pi * 2.0f; rads += rads_step)
				GL.Vertex3(Math.Sin((double)rads) * (double)size, Math.Cos((double)rads) * (double)size, 0.0f);
			GL.End();
		//	this.LineWidth = 1.0f;
		}
		public static void DrawSelectionSquare(float x1, float y1, float x2, float y2, Color unColor)
		{
			GL.Color4(unColor);
			GL.PolygonMode(MaterialFace.Front, PolygonMode.Line);
			GL.Rect(x1, y1, x2, y2);
		}
		public static void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, Color unColor)
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
	}
}

