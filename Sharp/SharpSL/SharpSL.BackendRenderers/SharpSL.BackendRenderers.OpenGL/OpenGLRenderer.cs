using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Drawing.Imaging;
using SharpAsset;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpSL.BackendRenderers.OpenGL
{
    public class OpenGLRenderer : IBackendRenderer
    {
        #region IBackendRenderer implementation

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
            GL.ReadPixels(viewport[0] + x, viewport[3] - (viewport[1] + y) + 5, width, height, OpenTK.Graphics.OpenGL.PixelFormat.Rgb, PixelType.UnsignedByte, pixel);
            return pixel;
        }

        public void SaveState()
        {
            GL.PushAttrib(AttribMask.EnableBit | AttribMask.ColorBufferBit);
        }

        public void RestoreState()
        {
            GL.PopAttrib();
        }
        public void SetStandardState()
        {
            GL.CullFace(CullFaceMode.Back);
            //GL.Disable (EnableCap.Blend);
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
            GL.Disable(EnableCap.Lighting);
            GL.Disable(EnableCap.LineStipple);
            GL.Disable(EnableCap.PolygonStipple);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.AlphaTest);
            GL.ShadeModel(ShadingModel.Flat);
        }

        public void GenerateBuffers(ref Shader shader)
        {
            shader.VertexID = GL.CreateShader(ShaderType.VertexShader);
            shader.FragmentID = GL.CreateShader(ShaderType.FragmentShader);
            shader.Program = GL.CreateProgram();
        }

        public void BindBuffers(ref Material mat)
        {
            var idLight = 0;
            if (mat.Shader.uniformArray.ContainsKey(UniformType.Float) && mat.Shader.uniformArray[UniformType.Float].ContainsKey("ambient"))
            {
                GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["ambient"], Sharp.Light.ambientCoefficient);
                foreach (var light in Sharp.Light.lights)
                {
                    GL.Uniform3(mat.Shader.uniformArray[UniformType.FloatVec3]["lights[" + idLight + "].position"], light.entityObject.Position);
                    //GL.UniformMatrix4(mat.Shader.uniformArray[UniformType.FloatMat4]["lights[" + idLight + "].modelMatrix"],false,ref light.entityObject.ModelMatrix);
                    GL.Uniform4(mat.Shader.uniformArray[UniformType.FloatVec4]["lights[" + idLight + "].color"], light.color);
                    GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].intensity"], light.intensity);
                    //GL.Uniform1(mat.Shader.uniformArray[UniformType.Float]["lights[" + idLight + "].angle"], light.angle);
                    idLight++;
                }
            }
            foreach (var matrixUniform in Material.globalMat4Array)
            {
                var mat4 = matrixUniform.Value;
                GL.UniformMatrix4(mat.Shader.uniformArray[UniformType.FloatMat4][matrixUniform.Key], false, ref mat4);
            }

            int texSlot = 0;
            foreach (var texUniform in Material.globalTexArray)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + texSlot);
                GL.BindTexture(TextureTarget.Texture2D, texUniform.Value);
                GL.Uniform1(mat.Shader.uniformArray[UniformType.Sampler2D][texUniform.Key], (int)TextureUnit.Texture0 + texSlot);
                texSlot++;
            }
            foreach (var texUniform in mat.texArray)
            {
                GL.ActiveTexture(TextureUnit.Texture0 + texSlot);
                GL.BindTexture(TextureTarget.Texture2D, texUniform.Value);
                GL.Uniform1(texUniform.Key, (int)TextureUnit.Texture0 + texSlot);
                texSlot++;
            }
            foreach (var matrixUniform in mat.mat4Array)
            {
                var mat4 = matrixUniform.Value;
                GL.UniformMatrix4(matrixUniform.Key, false, ref mat4);
            }
        }

        public void Use(ref Shader shader)
        {
            GL.UseProgram(shader.Program);
        }

        public void Allocate(ref Texture tex)
        {
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            BitmapData bmp_data = tex.bitmap.LockBits(new System.Drawing.Rectangle(0, 0, tex.bitmap.Width, tex.bitmap.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp_data.Width, bmp_data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, bmp_data.Scan0);

            tex.bitmap.UnlockBits(bmp_data);
        }

        public void GenerateBuffers(ref Texture tex)
        {
            if (tex.TBO == -1)
                tex.TBO = GL.GenTexture();
        }

        public void BindBuffers(ref Texture tex)
        {
            //GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, tex.TBO);
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
        public void Allocate<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible
        {
            //if (IsLoaded) return;
            //VBO
            //int tmpVBO;
            //GL.GenBuffers(1, out tmpVBO);
            //Console.WriteLine ("error check"+GL.DebugMessageCallback);
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var stride = Marshal.SizeOf(mesh.Vertices[0]);
            //var ptr = CustomConverter.ToPtr(mesh.Vertices,stride);
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Length * stride), IntPtr.Zero, (BufferUsageHint)mesh.UsageHint);
            var ptr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(mesh.Vertices.Length * stride), BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                Marshal.StructureToPtr(mesh.Vertices[i], IntPtr.Add(ptr, i * stride), false);
            }
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
            //Marshal.FreeHGlobal(ptr);
            watch.Stop();
            Console.WriteLine("cast: " + watch.ElapsedTicks);
            //int tmpEBO;
            //GL.GenBuffers(1, out tmpEBO);

            GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mesh.Indices.Length * Marshal.SizeOf(mesh.Indices[0])), ref mesh.Indices[0], (BufferUsageHint)mesh.UsageHint);

            //GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            /*// Generate Array Buffer Id
			mesh.TBO=GL.GenBuffer();
			// Bind current context to Array Buffer ID
			GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.TBO);
			// Send data to buffer
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Count * 8), shape.Texcoords, BufferUsageHint.StaticDraw);
*/
            // Validate that the buffer is the correct size
            //GL.GetBufferParameter(BufferTarget.ArrayBuffer, BufferParameterName.BufferSize, out bufferSize);
            //if (shape.Texcoords.Length * 8 != bufferSize)
            //	throw new ApplicationException("TexCoord array not uploaded correctly");

            // Clear the buffer Binding
            //GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            //IsLoaded = true;
        }
        public void Allocate(ref Shader shader)
        {
            if (shader.uniformArray.Count > 0)
                return;
            int status_code;
            string info;

            // Compile vertex shader
            GL.ShaderSource(shader.VertexID, shader.VertexSource); //make global frag and vert source, compile once and then mix them to shader
            GL.CompileShader(shader.VertexID);
            GL.GetShaderInfoLog(shader.VertexID, out info);
            GL.GetShader(shader.VertexID, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);

            // Compile fragment shader
            GL.ShaderSource(shader.FragmentID, shader.FragmentSource);
            GL.CompileShader(shader.FragmentID);
            GL.GetShaderInfoLog(shader.FragmentID, out info);
            GL.GetShader(shader.FragmentID, ShaderParameter.CompileStatus, out status_code);

            if (status_code != 1)
                throw new ApplicationException(info);


            GL.AttachShader(shader.Program, shader.FragmentID); //support multiple vert/frag shaders via foreach vert/frag source
            GL.AttachShader(shader.Program, shader.VertexID);
            GL.BindAttribLocation(shader.Program, 0, "vertex_position");
            GL.BindAttribLocation(shader.Program, 1, "vertex_color");
            GL.BindAttribLocation(shader.Program, 2, "vertex_texcoord");
            GL.BindAttribLocation(shader.Program, 3, "vertex_normal");

            GL.LinkProgram(shader.Program);


            int numOfUniforms = 0;
            GL.GetProgram(shader.Program, GetProgramParameterName.ActiveUniforms, out numOfUniforms);
            int num;
            GL.GetProgram(shader.Program, GetProgramParameterName.ActiveUniformMaxLength, out num);
            StringBuilder stringBuilder = new StringBuilder((num == 0) ? 1 : num);
            int size = 0;
            ActiveUniformType uniType;
            Console.WriteLine("start uni query");
            for (int i = 0; i < numOfUniforms; i++)
            {

                GL.GetActiveUniform(shader.Program, i, stringBuilder.Capacity, out num, out size, out uniType, stringBuilder);
                Console.WriteLine(stringBuilder.ToString() + " " + uniType + " : " + size);
                if (!shader.uniformArray.ContainsKey((UniformType)uniType))
                    shader.uniformArray.Add((UniformType)uniType, new System.Collections.Generic.Dictionary<string, int>() { [stringBuilder.ToString()] = i });
                else
                    shader.uniformArray[(UniformType)uniType].Add(stringBuilder.ToString(), i);
            }
            //GL.UseProgram(Program);

            //Console.WriteLine (attribType);
            //GL.UseProgram(0);
        }
        public void Use<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible
        {
            GL.DrawElements(PrimitiveType.Triangles, mesh.Indices.Length, (DrawElementsType)Mesh<IndexType>.indiceType, IntPtr.Zero);
        }
        public void Delete(ref Shader shader)
        {
            if (shader.Program != 0)
                GL.DeleteProgram(shader.Program);
            if (shader.FragmentID != 0)
                GL.DeleteShader(shader.FragmentID);
            if (shader.VertexID != 0)
                GL.DeleteShader(shader.VertexID);
        }
        public void Scissor(int x, int y, int width, int height)
        {
            GL.Scissor(x, y, width, height);
            GL.Viewport(x, y, width, height);
        }

        public void ClearBuffer()
        {
            GL.ClearColor(new OpenTK.Graphics.Color4(0.25f, 0.25f, 0.25f, 0f));
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        }

        public void SetupGraphic()
        {
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
            GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
            GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
        }

        public void GenerateBuffers<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible
        {
            mesh.VBO = GL.GenBuffer();
            mesh.EBO = GL.GenBuffer();
        }

        public void BindBuffers<IndexType>(ref Mesh<IndexType> mesh) where IndexType : struct, IConvertible
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, mesh.VBO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.EBO);
        }

        public void ChangeShader()
        {
            GL.UseProgram(0);
        }
        public void BindVertexAttrib(int stride, RegisterAsAttribute attrib)
        {
            GL.EnableVertexAttribArray(attrib.shaderLocation);
            GL.VertexAttribPointer(attrib.shaderLocation, attrib.Dimension, (VertexAttribPointerType)attrib.type, false, stride, attrib.offset);
        }

        #endregion
    }
}

