using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics;
using OpenTK;
using SharpAsset;
using System.Text;
using System.Collections.Generic;
using TupleExtensions;

namespace SharpSL.BackendRenderers.OpenGL
{
    public class OpenGLRenderer : IBackendRenderer
    {
        #region IBackendRenderer implementation

        public void CreateContext(Func<string, IntPtr> GetProcAddress, Func<IntPtr> GetCurrentContext)
        {
            new GraphicsContext(ContextHandle.Zero, (function) => GetProcAddress(function),
                                                                    () => new ContextHandle(GetCurrentContext()));
        }

        public void FinishCommands()
        {
            GL.Flush();
            GL.Finish();
            GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        }

        public byte[] ReadPixels(int x, int y, int width, int height)
        {
            byte[] pixel = new byte[3];
            int[] viewport = new int[4];
            // Flip Y-axis (Windows <-> OpenGL)
            GL.GetInteger(GetPName.Viewport, viewport);
            Console.WriteLine("view: " + viewport[0]);
            GL.ReadPixels(viewport[0] + x, viewport[3] - (viewport[1] + y) + 5, width, height, PixelFormat.Rgb, PixelType.UnsignedByte, pixel);

            return pixel;
        }

        public void SetStandardState()
        {
            GL.CullFace(CullFaceMode.Back);
            GL.Disable(EnableCap.Blend);
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Less);

            //GL.Enable(EnableCap.DebugOutput);
            //GL.Enable(EnableCap.DebugOutputSynchronous);
        }

        public void SetFlatColorState()
        {
            GL.Disable(EnableCap.Fog);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Dither);
            //GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.LineStipple);
            GL.Disable(EnableCap.PolygonStipple);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            GL.ShadeModel(ShadingModel.Flat);
        }

        public void GenerateBuffers(ref int Program, ref int VertexID, ref int FragmentID)
        {
            VertexID = GL.CreateShader(ShaderType.VertexShader);
            FragmentID = GL.CreateShader(ShaderType.FragmentShader);
            Program = GL.CreateProgram();
        }

        public void Use(int Program)
        {
            GL.UseProgram(Program);
        }

        public void Allocate(ref byte bitmap, int width, int height, bool ui = false)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            if (ui)
            {
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            }
            GL.TexImage2D(TextureTarget.Texture2D, 0, ui ? PixelInternalFormat.Alpha : PixelInternalFormat.Rgba, width, height, 0,
           ui ? PixelFormat.Alpha : PixelFormat.Bgra, PixelType.UnsignedByte, ref bitmap);
        }

        public void GenerateBuffers(ref int TBO)
        {
            if (TBO is -1)
                TBO = GL.GenTexture();
        }

        public void BindBuffers(ref int TBO)
        {
            //GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, TBO);
        }

        /*public void Do (Work whatToDo, ref Shader shader)
		{
			switch (whatToDo) {
			case Work.Allocate:
				Allocate (ref shader);
				break;

			case Work.Delete:
				Delete (ref shader);
				break;
			}
		}

		public void Do<IndexType> (Work whatToDo, ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible
		{
			switch (whatToDo) {
			case Work.Allocate:
				Allocate(ref mesh);
				break;

			case Work.Use:
				Use (ref mesh);
				break;
			}
		}*/

        public void Allocate(ref UsageHint usageHint, ref byte vertsMemAddr, ref byte indicesMemAddr, int vertsMemLength, int indicesMemLength)
        {
            //Console.WriteLine ("error check"+GL.DebugMessageCallback);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            //var ptr = CustomConverter.ToPtr(mesh.Vertices, stride);
            GL.BufferData(BufferTarget.ArrayBuffer, vertsMemLength, ref vertsMemAddr, (BufferUsageHint)usageHint);
            /*var ptr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(mesh.Vertices.Length * stride), BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                Marshal.StructureToPtr(mesh.Vertices[i], IntPtr.Add(ptr, i * stride), false);
            }
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);*/
            watch.Stop();
            Console.WriteLine("cast: " + watch.ElapsedTicks);

            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesMemLength, ref indicesMemAddr, (BufferUsageHint)usageHint);
        }

        public void Allocate(ref int Program, ref int VertexID, ref int FragmentID, ref string VertexSource, ref string FragmentSource, ref Dictionary<string, int> uniformArray)
        {
            //if (shader.uniformArray.Count > 0)
            //  return;
            int status_code;
            string info;
            // Compile vertex shader
            GL.ShaderSource(VertexID, VertexSource); //make global frag and vert source, compile once and then mix them to shader
            GL.CompileShader(VertexID);
            GL.GetShaderInfoLog(VertexID, out info);
            GL.GetShader(VertexID, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile fragment shader
            GL.ShaderSource(FragmentID, FragmentSource);
            GL.CompileShader(FragmentID);
            GL.GetShaderInfoLog(FragmentID, out info);
            GL.GetShader(FragmentID, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            GL.AttachShader(Program, FragmentID); //support multiple vert/frag shaders via foreach vert/frag source
            GL.AttachShader(Program, VertexID);
            GL.BindAttribLocation(Program, 0, "vertex_position");
            GL.BindAttribLocation(Program, 1, "vertex_color");
            GL.BindAttribLocation(Program, 2, "vertex_texcoord");
            GL.BindAttribLocation(Program, 3, "vertex_normal");

            GL.LinkProgram(Program);

            int numOfUniforms = 0;
            GL.GetProgram(Program, GetProgramParameterName.ActiveUniforms, out numOfUniforms);
            int num;
            GL.GetProgram(Program, GetProgramParameterName.ActiveUniformMaxLength, out num);
            StringBuilder stringBuilder = new StringBuilder((num == 0) ? 1 : num);
            int size = 0;
            ActiveUniformType uniType;
            Console.WriteLine("start uni query");
            for (int i = 0; i < numOfUniforms; i++)
            {
                GL.GetActiveUniform(Program, i, stringBuilder.Capacity, out num, out size, out uniType, stringBuilder);
                Console.WriteLine(stringBuilder.ToString() + " " + uniType + " : " + size);
                if (!uniformArray.ContainsKey(stringBuilder.ToString()))
                    uniformArray.Add(stringBuilder.ToString(), i);
            }
            //GL.UseProgram(Program);

            //Console.WriteLine (attribType);
            //GL.UseProgram(0);
        }

        public void Use(ref IndiceType indiceType, int length)
        {
            GL.DrawElements(PrimitiveType.Triangles, length, (DrawElementsType)indiceType, IntPtr.Zero);
            slot = 0;
        }

        public void Delete(ref int Program, ref int VertexID, ref int FragmentID)
        {
            if (Program != 0)
                GL.DeleteProgram(Program);
            if (FragmentID != 0)
                GL.DeleteShader(FragmentID);
            if (VertexID != 0)
                GL.DeleteShader(VertexID);
        }

        public void Viewport(int x, int y, int width, int height)
        {
            GL.Viewport(x, y, width, height);
            GL.Scissor(x, y, width, height);
        }

        public void Clip(int x, int y, int width, int height)
        {
            GL.Scissor(x, y, width, height);
        }

        public void ClearColor()
        {
            ClearColor(0.15f, 0.15f, 0.15f, 0f);//use 0.12f for background 0.25f use for ui elem
            //ClearColor(1f, 1f, 1f, 0f);
        }

        public void ClearColor(float r, float g, float b, float a)
        {
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.AlphaTest);
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

            GL.ClearColor(r, g, b, a);
        }

        public void ClearBuffer()
        {
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        }

        public void ClearDepth()
        {
            //GL.Enable(EnableCap.Blend);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        public void EnableScissor()
        {
            GL.Enable(EnableCap.ScissorTest);
        }

        public void SetupGraphic()
        {
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
        }

        public void GenerateBuffers(ref int VBO, ref int EBO)
        {
            VBO = GL.GenBuffer();
            EBO = GL.GenBuffer();
        }

        public void BindBuffers(ref int VBO, ref int EBO)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
        }

        public void ChangeShader()
        {
            GL.UseProgram(0);
        }

        public void WriteDepth(bool enable = true)
        {
            GL.DepthMask(enable);
        }

        public void BindVertexAttrib(ref AttributeType type, int shaderLoc, int dim, int stride, int offset)
        {
            GL.EnableVertexAttribArray(shaderLoc);
            GL.VertexAttribPointer(shaderLoc, dim, (VertexAttribPointerType)type, false, stride, offset);
        }

        public void SendMatrix4(int location, ref float mat)
        {
            //GL.UniformMatrix4(location, mat.Length, false, ref mat[0].Row0.X);
            GL.UniformMatrix4(location, 1, false, ref mat);
        }

        private int slot = 0;

        public void SendTexture2D(int location, ref int tbo)
        {
            //GL.ActiveTexture(TextureUnit.Texture0 + slot);//bugged?
            GL.BindTexture(TextureTarget.Texture2D, tbo);
            GL.Uniform1(location, (int)TextureUnit.Texture0 + slot);
            slot = ++slot;
        }

        public void SendUniform1(int location, ref float data)
        {
            GL.Uniform1(location, 1, ref data);
        }

        public void SendUniform2(int location, ref float data)
        {
            GL.Uniform2(location, 1, ref data);
        }

        public void SendUniform3(int location, ref float data)
        {
            GL.Uniform3(location, 1, ref data);
        }

        public void SendUniform4(int location, ref float data)
        {
            GL.Uniform4(location, 1, ref data);
        }

        #endregion IBackendRenderer implementation
    }
}