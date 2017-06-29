using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpAsset;
using SharpAsset.Pipeline;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SharpSL.BackendRenderers.OpenGL
{
    public class EditorOpenGLRenderer : IEditorBackendRenderer
    {
        public void UnloadMatrix()
        {
            GL.PopMatrix();
        }

        public void LoadMatrix(ref Matrix4 mat)
        {
            GL.LoadMatrix(ref mat);
            GL.PushMatrix();
        }

        public void DrawRotateGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor)
        {
            //this.SetupGraphic();
            GL.LineWidth(thickness);
            this.DrawCircle(3.5f * scale, 3f, ref yColor, Plane.Y);
            this.DrawCircle(3.5f * scale, 3f, ref zColor, Plane.Z);
            this.DrawCircle(3.5f * scale, 3f, ref xColor, Plane.X);
            GL.LineWidth(1f);
        }

        public void DrawTranslateGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor)
        {
            this.DrawLine(0f, 0f, 0f, 0f, 3f * scale, 0f, ref yColor, thickness);
            this.DrawConeY(0.2f * scale, 0.5f * scale, 3f * scale);
            this.DrawPlaneXZ(1f * scale, 0.3f * scale, ref yColor);
            this.DrawLine(0f, 0f, 0f, 3f * scale, 0f, 0f, ref xColor, thickness);
            this.DrawConeX(0.2f * scale, 0.5f * scale, 3f * scale);
            this.DrawPlaneZY(1f * scale, 0.3f * scale, ref xColor);
            this.DrawLine(0f, 0f, 0f, 0f, 0f, 3f * scale, ref zColor, thickness);
            this.DrawConeZ(0.2f * scale, 0.5f * scale, 3f * scale);
            this.DrawPlaneYX(1f * scale, 0.3f * scale, ref zColor);
            GL.Color4(0, 0, 0, 0);
        }

        public void DrawScaleGizmo(float thickness, float scale, ref byte xColor, ref byte yColor, ref byte zColor, Vector3 offset)
        {
            this.DrawCube(scale / 2f, -scale / 4f, offset.Y != 0 ? offset.Y : 3.5f * scale, scale / 4f, ref yColor);
            this.DrawCube(scale / 2f, -scale / 4f, -scale / 4f, offset.Z != 0 ? offset.Z : 4f * scale, ref zColor);
            this.DrawCube(scale / 2f, offset.X != 0 ? offset.X : 3.5f * scale, -scale / 4f, scale / 4f, ref xColor);
            GL.Color4(0, 0, 0, 0);
        }

        public void DrawLine(float v1x, float v1y, float v1z, float v2x, float v2y, float v2z, ref byte unColor, float width = 1f)
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

        public void DrawDiamond(Vector2 pos, Vector2 dir, ref byte color)
        {
            GL.Color4(ref color);
            GL.Begin(BeginMode.Quads);
            GL.Vertex2(pos + new Vector2(0, dir.Y));
            GL.Vertex2(pos + new Vector2(dir.X, 0));
            GL.Vertex2(pos - new Vector2(0, dir.Y));
            GL.Vertex2(pos - new Vector2(dir.X, 0));
            GL.End();
            GL.Color4(0, 0, 0, 0);
        }

        public void DrawSlicedQuad(float x1, float y1, float x2, float y2, float left, float right, float top, float bottom, ref byte unColor)
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

            #region old

            /*
            //1st square
            GL.TexCoord2(0, 0);
            GL.Vertex3(x1, y1, 0);

            GL.TexCoord2(0, bottom / height);
            GL.Vertex3(x1, y1 + bottom, 0);

            GL.TexCoord2(left / width, bottom / height);
            GL.Vertex3(x1 + left, y1 + bottom, 0);

            GL.TexCoord2(left / width, 0);
            GL.Vertex3(x1 + left, y1, 0);

            //2nd square
            GL.TexCoord2(0, bottom / height);
            GL.Vertex3(x1, y1 + bottom, 0);

            GL.TexCoord2(0, 1 - (top / height));
            GL.Vertex3(x1, y2 - top, 0);

            GL.TexCoord2(left / width, 1 - (top / height));
            GL.Vertex3(x1 + left, y2 - top, 0);

            GL.TexCoord2(left / width, bottom / height);
            GL.Vertex3(x1 + left, y1 + bottom, 0);

            //3rd square
            GL.TexCoord2(0, 1 - (top / height));
            GL.Vertex3(x1, y2 - top, 0);

            GL.TexCoord2(0, 1);
            GL.Vertex3(x1, y2, 0);

            GL.TexCoord2(left / width, 1);
            GL.Vertex3(x1 + left, y2, 0);

            GL.TexCoord2(left / width, 1 - (top / height));
            GL.Vertex3(x1 + left, y2 - top, 0);

            //4th square
            GL.TexCoord2(left / width, 1 - (top / height));
            GL.Vertex3(x1 + left, y2 - top, 0);

            GL.TexCoord2(left / width, 1);
            GL.Vertex3(x1 + left, y2, 0);

            GL.TexCoord2(1 - (right / width), 1);
            GL.Vertex3(x2 - right, y2, 0);

            GL.TexCoord2(1 - (right / width), 1 - (top / height));
            GL.Vertex3(x2 - right, y2 - top, 0);

            //5th square
            GL.TexCoord2(left / width, bottom / height);
            GL.Vertex3(x1 + left, y1 + bottom, 0);

            GL.TexCoord2(left / width, 1 - (top / height));
            GL.Vertex3(x1 + left, y2 - top, 0);

            GL.TexCoord2(1 - (right / width), 1 - (top / height));
            GL.Vertex3(x2 - right, y2 - top, 0);

            GL.TexCoord2(1 - (right / width), bottom / height);
            GL.Vertex3(x2 - right, y1 + bottom, 0);

            //6th
            GL.TexCoord2(left / width, 0);
            GL.Vertex3(x1 + left, y1, 0);

            GL.TexCoord2(left / width, bottom / height);
            GL.Vertex3(x1 + left, y1 + bottom, 0);

            GL.TexCoord2(1 - (right / width), bottom / height);
            GL.Vertex3(x2 - right, y1 + bottom, 0);

            GL.TexCoord2(1 - (right / width), 0);
            GL.Vertex3(x2 - right, y1, 0);

            //7th
            GL.TexCoord2(1 - (right / width), 1 - (top / height));
            GL.Vertex3(x2 - right, y2 - top, 0);

            GL.TexCoord2(1 - (right / width), 1);
            GL.Vertex3(x2 - right, y2, 0);

            GL.TexCoord2(1, 1);
            GL.Vertex3(x2, y2, 0);

            GL.TexCoord2(1, 1 - (top / height));
            GL.Vertex3(x2, y2 - top, 0);

            //8th
            GL.TexCoord2(1 - (right / width), bottom / height);
            GL.Vertex3(x2 - right, y1 + bottom, 0);

            GL.TexCoord2(1 - (right / width), 1 - (top / height));
            GL.Vertex3(x2 - right, y2 - top, 0);

            GL.TexCoord2(1, 1 - (top / height));
            GL.Vertex3(x2, y2 - top, 0);

            GL.TexCoord2(1, bottom / height);
            GL.Vertex3(x2, y1 + bottom, 0);

            //9th
            GL.TexCoord2(1 - (right / width), 0);
            GL.Vertex3(x2 - right, y1, 0);

            GL.TexCoord2(1 - (right / width), bottom / height);
            GL.Vertex3(x2 - right, y1 + bottom, 0);

            GL.TexCoord2(1, bottom / height);
            GL.Vertex3(x2, y1 + bottom, 0);

            GL.TexCoord2(1, 0);
            GL.Vertex3(x2, y1, 0);*/

            #endregion old

            /*
                        //1st square
                        // GL.TexCoord2(0, 0);
                        GL.Vertex3(x1, y1, 0);

                        //GL.TexCoord2(left / width, 0);
                        GL.Vertex3(x1 + left, y1, 0);

                        // GL.TexCoord2(1 - (right / width), 0);
                        GL.Vertex3(x2 - right, y1, 0);

                        // GL.TexCoord2(1, 0);
                        GL.Vertex3(x2, y1, 0);

                        // GL.TexCoord2(1, bottom / height);
                        GL.Vertex3(x2, y1 + bottom, 0);

                        // GL.TexCoord2(1, 1 - (top / height));
                        GL.Vertex3(x2, y2 - top, 0);

                        // GL.TexCoord2(1, 1);
                        GL.Vertex3(x2, y2, 0);

                        // GL.TexCoord2(1 - (right / width), 1);
                        GL.Vertex3(x2 - right, y2, 0);

                        //GL.TexCoord2(left / width, 1);
                        GL.Vertex3(x1 + left, y2, 0);

                        // GL.TexCoord2(0, 1);
                        GL.Vertex3(x1, y2, 0);

                        // GL.TexCoord2(0, 1 - (top / height));
                        GL.Vertex3(x1, y2 - top, 0);

                        // GL.TexCoord2(0, 1 - (top / height));
                        GL.Vertex3(x1, y2 - top, 0);

                        // GL.TexCoord2(left / width, bottom / height);
                        GL.Vertex3(x1 + left, y1 + bottom, 0);

                        // GL.TexCoord2(1 - (right / width), bottom / height);
                        GL.Vertex3(x2 - right, y1 + bottom, 0);

                        // GL.TexCoord2(1 - (right / width), 1 - (top / height));
                        GL.Vertex3(x2 - right, y2 - top, 0);

                        //GL.TexCoord2(left / width, 1 - (top / height));
                        GL.Vertex3(x1 + left, y2 - top, 0);*/

            GL.End();
        }

        public void DrawQuad(float x1, float y1, float x2, float y2, ref byte unColor)
        {
            // GL.Color4(ref unColor);

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
        }

        public void DrawCircle(float size, float lineWidth, ref byte unColor, Plane plane, float angle = 360, bool filled = false)
        {
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.Color4(ref unColor);
            GL.Begin(filled ? PrimitiveType.TriangleFan : PrimitiveType.LineLoop);
            float num = MathHelper.Pi * 0.01f;
            float fraction = MathHelper.DegreesToRadians(Math.Abs(angle)) / MathHelper.TwoPi;

            GL.Vertex3(0.0, 0.0, 0.0);

            for (float num2 = 0; num2 < MathHelper.TwoPi * fraction + num; num2 += num)
            {
                switch (plane)
                {
                    case Plane.X: GL.Vertex3(0.0, Math.Sin((double)num2) * (double)size, Math.Cos((double)num2) * (double)size); break;
                    case Plane.Y: GL.Vertex3(Math.Sin((double)num2) * (double)size, 0.0, Math.Cos((double)num2) * (double)size); break;
                    case Plane.Z: GL.Vertex3(Math.Sin((double)num2) * (double)size, Math.Cos((double)num2) * (double)size, 0.0); break;
                }
            }

            GL.End();
        }

        public void DrawLine(Vector3 v1, Vector3 v2, ref byte color)
        {
            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);//one|dstcolor, oneminusscrcolor|dstcolor,srcalpha|dstcolor,oneminussrcalpha|dstcolor
            DrawLine(v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, ref color);
        }

        public void DrawFilledPolyline(float size, float lineWidth, ref byte color, ref Matrix4 mat, ref Vector3[] vecArray, bool fan = true)
        {
            //TurnOnDebugging();

            GL.Enable(EnableCap.Blend);

            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
            GL.LoadIdentity();
            GL.LoadMatrix(ref mat);
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

        public void DrawSphere(float radius, int lats, int longs, ref byte unColor)
        {
            GL.Color4(ref unColor);
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

        public void DrawCube(float size, float posX, float posY, float posZ, ref byte unColor)
        {
            float[] vertices =
            {
                 0.0f, 0.0f, 0.0f,
                 1.0f, 0.0f, 0.0f,
                 1.0f, 1.0f, 0.0f,
                0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, -1.0f,
                1.0f, 0.0f, -1.0f,
                1.0f, 1.0f, -1.0f,
                0.0f, 1.0f, -1.0f,
                 1.0f, 0.0f, 0.0f,
               1.0f, 0.0f, -1.0f,
               1.0f, 1.0f, -1.0f,
               1.0f, 1.0f, 0.0f,
               0.0f, 0.0f, 0.0f,
               0.0f, 0.0f, -1.0f,
               0.0f, 1.0f, -1.0f,
               0.0f, 1.0f, 0.0f,
               0.0f, 1.0f, 0.0f,
               1.0f, 1.0f, 0.0f,
               1.0f, 1.0f, -1.0f,
               0.0f, 1.0f, -1.0f,
               0.0f, 0.0f, 0.0f,
               1.0f, 0.0f, 0.0f,
               1.0f, 0.0f, -1.0f,
               0.0f, 0.0f, -1.0f
            };
            GL.Color4(ref unColor);
            GL.Begin(PrimitiveType.Quads);
            for (int i = 0; i < vertices.Length; i += 3)
                GL.Vertex3(vertices[i] * size + posX, (vertices[i + 1]) * size + posY, (vertices[i + 2]) * size + posZ);
            GL.End();
        }

        public void DrawPlaneXZ(float size, float sizeOffset, ref byte unColor)
        {
            size += sizeOffset;
            GL.Color4(ref unColor);
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex3(sizeOffset, 0f, sizeOffset);
            GL.Vertex3(size, 0f, sizeOffset);
            GL.Vertex3(size, 0f, size);
            GL.Vertex3(sizeOffset, 0f, size);
            GL.End();
        }

        public void DrawPlaneZY(float size, float sizeOffset, ref byte unColor)
        {
            size += sizeOffset;
            GL.Color4(ref unColor);
            GL.Begin(PrimitiveType.Quads);
            GL.Vertex3(0f, sizeOffset, sizeOffset);
            GL.Vertex3(0f, size, sizeOffset);
            GL.Vertex3(0f, size, size);
            GL.Vertex3(0f, sizeOffset, size);
            GL.End();
        }

        public void DrawPlaneYX(float size, float sizeOffset, ref byte unColor)
        {
            size += sizeOffset;
            GL.Color4(ref unColor);
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

        public void DrawGrid(ref byte color, Vector3 pos, float X, float Y, ref Matrix4 projMat, int cell_size = 16, int grid_size = 2560)
        {
            GL.UseProgram(0);
            int num = (int)Math.Round((double)(pos.X / (float)cell_size)) * cell_size;
            int num2 = (int)Math.Round((double)(pos.Y / (float)cell_size)) * cell_size;
            int num3 = grid_size / cell_size;
            GL.LoadMatrix(ref projMat);
            GL.PushMatrix();
            GL.Translate((float)num - (float)grid_size / 2f, 0f, (float)num2 - (float)grid_size / 2f);
            GL.Color4(ref color);
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

        private void bvh_to_vertices(Bone joint, ref List<Vector4> vertices,
            ref List<ushort> indices, ref List<Matrix4> matrices,
            ushort parentIndex = 0)
        {
            // vertex from current joint is in 4-th ROW (column-major ordering)
            var translatedVertex = joint.Offset.Inverted().Column3;//check column if wrong
            matrices.Add(joint.Offset.Inverted());
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
            List<Matrix4> matrices = new List<Matrix4>();

            bvh_to_vertices(skele.bones[0], ref vertices, ref bvhindices, ref matrices);

            GL.BindBuffer(BufferTarget.ArrayBuffer, skele.VBOV);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(Marshal.SizeOf<Vector4>() * vertices.Count), vertices.ToArray(), BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void newinit(ref Skeleton skele)
        {
            GL.Enable(EnableCap.DepthTest);

            List<Vector4> vertices = new List<Vector4>();
            List<Matrix4> matriceses = new List<Matrix4>();
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
            List<Matrix4> matrices = new List<Matrix4>();
            List<ushort> bvhindices = new List<ushort>();

            bvh_to_vertices(skele.bones[0], ref vertices, ref bvhindices, ref matrices);
            var bvh_elements = bvhindices.Count;
            var mats = matrices.ToArray();
            //GL.Enable (EnableCap.Light0);
            //GL.light
            //GL.LoadMatrix(ref skele.MVP);
            //GL.ShadeModel (ShadingModel.Flat);
            GL.LoadMatrix(ref skele.MVP);
            //foreach (var childBone in skele.bones[0].Children)
            DisplayOcta(skele.bones[0]);
            var skeleShader = Pipeline.GetPipeline<ShaderPipeline>().GetAsset("SkeletonShader");
            GL.UseProgram(skeleShader.Program);
            GL.UniformMatrix4(GL.GetUniformLocation(skeleShader.Program, "mvp_matrix"), false, ref skele.MVP);

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
            var mat = bone.Offset.Inverted();
            var head = mat.Column3.Xyz;
            var tail = bone.Children.Count > 0 ? bone.Children[0].Offset.Inverted().Column3.Xyz : Vector4.Transform(Vector4.Zero, mat).Xyz; //or borrow last length
                                                                                                                                            //Console.WriteLine ((copyMat-final).Length);
            draw_bone_solid_octahedral(ref mat, (head - tail).Length);
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

        private static void draw_bone_solid_octahedral(ref Matrix4 mat, float length)
        {
            //Console.WriteLine ("buuu");
            //	displist = GL.GenLists(1);
            //GL.NewList(displist,ListMode.Compile);

            GL.PushMatrix();

            GL.MultTransposeMatrix(ref mat);
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