using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using SharpAsset;
using Squid;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace SharpSL.BackendRenderers.OpenGL
{
	public class OpenGLRenderer : IBackendRenderer
	{
		#region IBackendRenderer implementation

		public Func<IntPtr, IntPtr, int> MakeCurrent { get; set; }
		public Action<IntPtr> SwapBuffers { get; set; }
		public uint currentWindow { get; set; }

		public OpenGLRenderer()
		{
		}

		public void Start()
		{
			Toolkit.Init();
		}

		public IntPtr CreateContext(Func<string, IntPtr> GetProcAddress, Func<IntPtr> GetCurrentContext)//add createConext func?
		{
			new GraphicsContext(ContextHandle.Zero, (function) => GetProcAddress(function),
																	() => new ContextHandle(GetCurrentContext()));
			return GetCurrentContext();
		}

		public void FinishCommands()
		{
			//GL.Flush();
			//GL.Finish();
			// GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
		}

		public byte[] ReadPixels(int x, int y, int width, int height, TextureFormat pxFormat)
		{
			(var pixelFormat, byte[] pixel) = pxFormat switch
			{
				TextureFormat.R => (PixelFormat.Red, new byte[3]),
				TextureFormat.A => (PixelFormat.Alpha, new byte[3]),
				TextureFormat.DepthFloat => (PixelFormat.DepthComponent, new byte[3]),
				TextureFormat.RGB => (PixelFormat.Rgb, new byte[3]),
				TextureFormat.RGBA => (PixelFormat.Rgba, new byte[4]),
				TextureFormat.RUInt => (PixelFormat.RedInteger, new byte[3]),
				TextureFormat.RGUInt => (PixelFormat.RgInteger, new byte[Marshal.SizeOf<uint>() * 2]),
				TextureFormat.RGBAFloat => (PixelFormat.Rgba, new byte[3]),
				TextureFormat.RG16_SNorm => (PixelFormat.Rgb, new byte[3]),
				_ => (PixelFormat.Bgra, new byte[4])
			};
			// Flip Y-axis (Windows <-> OpenGL)
			GL.ReadPixels(x, y, width, height, pixelFormat, PixelType.UnsignedByte, pixel);

			return pixel;
		}
		public void SetStandardState()
		{
			GL.DepthMask(true);
			GL.CullFace(CullFaceMode.Back);
			GL.Disable(EnableCap.Blend);
			GL.Enable(EnableCap.DepthTest);
			GL.DepthFunc(DepthFunction.Less);

			//GL.Enable(EnableCap.DebugOutput);
			//GL.Enable(EnableCap.DebugOutputSynchronous);
		}

		public void SetFlatColorState()
		{
			GL.Disable(EnableCap.Texture2D);
			GL.Disable(EnableCap.Dither);
			GL.Enable(EnableCap.DepthTest);
			//GL.Disable(EnableCap.DepthTest);
			GL.Disable(EnableCap.CullFace);
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.AlphaTest);
		}

		public void Use(int Program)
		{
			GL.UseProgram(Program);
		}

		public void Allocate(ref byte bitmap, int width, int height, TextureFormat pxFormat)
		{
			var (internalPixelFormat, pixelFormat, pixelType) = pxFormat switch
			{
				TextureFormat.R => (PixelInternalFormat.R16, PixelFormat.Red, PixelType.UnsignedByte),
				TextureFormat.A => (PixelInternalFormat.Alpha, PixelFormat.Alpha, PixelType.UnsignedByte),
				TextureFormat.DepthFloat => (PixelInternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedByte),
				TextureFormat.RGB => (PixelInternalFormat.Rgb, PixelFormat.Rgb, PixelType.UnsignedByte),
				TextureFormat.RUInt => (PixelInternalFormat.R32ui, PixelFormat.RedInteger, PixelType.UnsignedByte),
				TextureFormat.RGUInt => (PixelInternalFormat.Rg32ui, PixelFormat.RgInteger, PixelType.UnsignedByte),
				TextureFormat.RGBAFloat => (PixelInternalFormat.Rgba32f, PixelFormat.Rgba, PixelType.UnsignedByte),
				TextureFormat.RG16_SNorm => (PixelInternalFormat.Rg16Snorm, PixelFormat.Rgb, PixelType.UnsignedByte),
				_ => (PixelInternalFormat.Rgba, PixelFormat.Bgra, PixelType.UnsignedByte)
			};
			GL.TexImage2D(TextureTarget.Texture2D, 0, internalPixelFormat, width, height, 0,
		   pixelFormat, pixelType, ref bitmap);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			if (pxFormat is TextureFormat.DepthFloat)
			{
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareFunc, (int)DepthFunction.Lequal);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureCompareMode, (int)TextureCompareMode.None);
			}
		}
		public void GenerateBuffers(Target target, out int id)
		{
			id = target switch
			{
				Target.Texture => GL.GenTexture(),
				Target.Frame => GL.GenFramebuffer(),
				Target.Shader => GL.CreateProgram(),
				Target.VertexShader => GL.CreateShader(ShaderType.VertexShader),
				Target.FragmentShader => GL.CreateShader(ShaderType.FragmentShader),
				_ => GL.GenBuffer(),
			};
		}

		public void BindBuffers(Target target, int id)
		{
			switch (target)
			{
				case Target.Texture: GL.BindTexture(TextureTarget.Texture2D, id); break;
				case Target.Mesh: GL.BindBuffer(BufferTarget.ArrayBuffer, id); break;
				case Target.Indices: GL.BindBuffer(BufferTarget.ElementArrayBuffer, id); break;
				case Target.Shader: GL.UseProgram(id); break;
				case Target.Frame: GL.BindFramebuffer(FramebufferTarget.Framebuffer, id); break;
				case Target.WriteFrame: GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, id); break;
				case Target.ReadFrame: GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, id); break;
			}
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

		public void Allocate(Target target, UsageHint usageHint, ref byte addr, int length, bool reuse = false)
		{
			switch (target)
			{
				case Target.Indices:
					if (reuse) GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, length, ref addr);
					else
						GL.BufferData(BufferTarget.ElementArrayBuffer, length, ref addr, (BufferUsageHint)usageHint);
					break;
				case Target.Mesh:
					if (reuse) GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, length, ref addr);
					else
						GL.BufferData(BufferTarget.ArrayBuffer, length, ref addr, (BufferUsageHint)usageHint); break;
			}
			//Console.WriteLine ("error check"+GL.DebugMessageCallback);
			/*var watch = System.Diagnostics.Stopwatch.StartNew();
			//var ptr = CustomConverter.ToPtr(mesh.Vertices, stride);
			
			var ptr = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(mesh.Vertices.Length * stride), BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit | BufferAccessMask.MapFlushExplicitBit);
            for (int i = 0; i < mesh.Vertices.Length; i++)
            {
                Marshal.StructureToPtr(mesh.Vertices[i], IntPtr.Add(ptr, i * stride), false);
            }
            GL.UnmapBuffer(BufferTarget.ArrayBuffer);
			watch.Stop();
			Console.WriteLine("cast: " + watch.ElapsedTicks);*/
		}

		public void Allocate(int Program, int VertexID, int FragmentID, string VertexSource, string FragmentSource, Dictionary<string, int> uniformArray, Dictionary<string, (int location, int size)> attribArray)
		{
			//if (shader.uniformArray.Count > 0)
			//  return;
			int status_code;
			string info;
			// Compile vertex shader
			GL.ShaderSource(VertexID, VertexSource); //make global frag and vert source, compile once and then mix them to shader
			GL.CompileShader(VertexID);
			var message = "OpenGL version is " + GL.GetString(StringName.Version);
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

			GL.AttachShader(Program, FragmentID); //TODO: support multiple vert/frag shaders via foreach vert/frag source
			GL.AttachShader(Program, VertexID);

			/*GL.BindAttribLocation(Program, 0, "vertex_position");
			GL.BindAttribLocation(Program, 1, "vertex_texcoord");
			GL.BindAttribLocation(Program, 2, "vertex_normal");
			GL.BindAttribLocation(Program, 3, "prev_position");
			GL.BindAttribLocation(Program, 4, "next_position");
			GL.BindAttribLocation(Program, 5, "dir");*/
			GL.LinkProgram(Program);
			GL.GetProgram(Program, GetProgramParameterName.ActiveAttributes, out var numOfAttribs);
			GL.GetProgram(Program, GetProgramParameterName.ActiveAttributeMaxLength, out var numA);
			StringBuilder stringBuilder = new StringBuilder((numA == 0) ? 1 : numA);
			ActiveAttribType attribType;
			Console.WriteLine("start attrib query");
			for (int i = 0; i < numOfAttribs; i++)
			{
				GL.GetActiveAttrib(Program, i, stringBuilder.Capacity, out _, out var size, out attribType, stringBuilder);
				Console.WriteLine(stringBuilder.ToString() + " " + attribType + " : " + size);
				if (!attribArray.ContainsKey(stringBuilder.ToString()))
					attribArray.Add(stringBuilder.ToString(), (GL.GetAttribLocation(Program, stringBuilder.ToString()), size)); //TODO: change to bindAttrib via parsing all vertex formats and binding all fields?
			}

			GL.GetProgram(Program, GetProgramParameterName.ActiveUniforms, out var numOfUniforms);
			GL.GetProgram(Program, GetProgramParameterName.ActiveUniformMaxLength, out var numU);
			stringBuilder = new StringBuilder((numU == 0) ? 1 : numU);
			ActiveUniformType uniType;
			Console.WriteLine("start uni query");
			for (int i = 0; i < numOfUniforms; i++)
			{
				GL.GetActiveUniform(Program, i, stringBuilder.Capacity, out _, out var size, out uniType, stringBuilder);
				Console.WriteLine(stringBuilder.ToString() + " " + uniType + " : " + size);
				if (!uniformArray.ContainsKey(stringBuilder.ToString()))
					uniformArray.Add(stringBuilder.ToString(), i);
			}
		}

		public void Draw(IndiceType indiceType, int length)
		{

			GL.DrawElements(PrimitiveType.Triangles, length, (DrawElementsType)indiceType, IntPtr.Zero);
			slot = 0;
		}

		public void Delete(int Program, int VertexID, int FragmentID)
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
			// GL.Scissor(x, y, width, height);
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
		public void WriteDepth(bool enable = true)
		{
			GL.DepthMask(enable);
		}

		public void BindVertexAttrib(AttributeType type, int shaderLoc, int dim, int stride, int offset)
		{

			GL.VertexAttribPointer(shaderLoc, dim, (VertexAttribPointerType)type, false, stride, offset);
			GL.EnableVertexAttribArray(shaderLoc);
		}

		public void SendMatrix4(int location, ref byte data)
		{
			//GL.UniformMatrix4(location, mat.Length, false, ref mat[0].Row0.X);
			GL.UniformMatrix4(location, 1, false, ref Unsafe.As<byte, float>(ref data));
		}

		private static int slot = 0;

		public void SendTexture2D(int location, ref byte tbo)
		{
			GL.ActiveTexture(TextureUnit.Texture0 + slot);
			GL.BindTexture(TextureTarget.Texture2D, Unsafe.As<byte, int>(ref tbo));
			GL.Uniform1(location, slot);
			slot = ++slot;
		}
		public void BindRenderTexture(int tbo, TextureRole role)
		{
			var attachment = role switch//TODO: ensure theres always 1x color attachment even when only depth is needed since some graphic cards fail when theres no valid color attach
			{
				TextureRole.Color0 => FramebufferAttachment.ColorAttachment0,
				TextureRole.Color1 => FramebufferAttachment.ColorAttachment1,
				TextureRole.Depth => FramebufferAttachment.DepthAttachment,
				TextureRole.Stencil => FramebufferAttachment.StencilAttachment,
				_ => FramebufferAttachment.ColorAttachment0
			};
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, attachment, TextureTarget.Texture2D, tbo, 0);
		}
		public void SendUniform1(int location, ref byte data)
		{
			GL.Uniform1(location, 1, ref Unsafe.As<byte, float>(ref data));
		}

		public void SendUniformFloat2(int location, ref byte data)
		{
			GL.Uniform2(location, 1, ref Unsafe.As<byte, float>(ref data));
		}
		public void SendUniformUInt2(int location, ref byte data)
		{
			GL.Uniform2(location, 1, ref Unsafe.As<byte, uint>(ref data));
		}

		public void SendUniform3(int location, ref byte data)
		{
			GL.Uniform3(location, 1, ref Unsafe.As<byte, float>(ref data));
		}

		public void SendUniform4(int location, ref byte data)
		{
			GL.Uniform4(location, 1, ref Unsafe.As<byte, float>(ref data));
		}

		#endregion IBackendRenderer implementation
	}
}