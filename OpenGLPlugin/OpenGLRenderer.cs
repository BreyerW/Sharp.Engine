using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using PluginAbstraction;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ParameterType = PluginAbstraction.ParameterType;

namespace SharpSL.BackendRenderers.OpenGL
{
	public class OpenGLRenderer : IBackendRenderer, IPlugin
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
			//var bindings = new GLBindings() { getProcAddr = GetProcAddress };
			//GL.LoadBindings(bindings);
			return GetCurrentContext();
		}
		public void GetQueryResult(int id, out int result)
		{
			GL.GetQueryObject(id, GetQueryObjectParam.QueryResult, out result);
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
		public void GenerateBuffers(Target target, Span<int> id)
		{
			switch (target)
			{
				case Target.Texture: GL.GenTextures(id.Length, out id[0]); break;
				case Target.Frame: GL.GenFramebuffers(id.Length, out id[0]); break;
				case Target.Shader: id[0] = GL.CreateProgram(); break;
				case Target.VertexShader: id[0] = GL.CreateShader(ShaderType.VertexShader); break;
				case Target.FragmentShader: id[0] = GL.CreateShader(ShaderType.FragmentShader); break;
				case Target.OcclusionQuery: GL.GenQueries(id.Length, out id[0]); break;
				default: GL.GenBuffers(id.Length, out id[0]); break;
			};
		}
		public void DeleteBuffers(Target target, Span<int> id)
		{
			switch (target)
			{
				case Target.Texture: GL.DeleteTextures(id.Length, ref id[0]); break;
				case Target.Frame: GL.DeleteFramebuffers(id.Length, ref id[0]); break;
				case Target.Shader: GL.DeleteProgram(id[0]); break;
				case Target.VertexShader: GL.DeleteShader(id[0]); break;
				case Target.FragmentShader: GL.DeleteShader(id[0]); break;
				case Target.OcclusionQuery: GL.DeleteQueries(id.Length, ref id[0]); break;
				default: GL.DeleteBuffers(id.Length, ref id[0]); break;
			};
		}
		public void DeleteBuffer(Target target, int id)
		{
			switch (target)
			{
				case Target.Texture: GL.DeleteTexture(id); break;
				case Target.Frame: GL.DeleteFramebuffer(id); break;
				case Target.Shader: GL.DeleteProgram(id); break;
				case Target.VertexShader: GL.DeleteShader(id); break;
				case Target.FragmentShader: GL.DeleteShader(id); break;
				case Target.OcclusionQuery: GL.DeleteQuery(id); break;
				default: GL.DeleteBuffer(id); break;
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

		public void Allocate(int Program, int VertexID, int FragmentID, string VertexSource, string FragmentSource, Dictionary<string, int> uniformArray, Dictionary<int, (ParameterType location, int size)> attribArray)
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
				GL.GetActiveAttrib(Program, i, stringBuilder.Capacity, out _, out var size, out attribType, out var name);
				Console.WriteLine(name + " " + attribType + " : " + size);
				var loc = GL.GetAttribLocation(Program, name);
				if (!uniformArray.ContainsKey(name))
					uniformArray.Add(name, loc);
				if (!attribArray.ContainsKey(loc))
					attribArray.Add(loc, (ConvertAttributeToParameterType((ActiveUniformType)attribType), size)); //TODO: change to bindAttrib via parsing all vertex formats and binding all fields?
			}

			GL.GetProgram(Program, GetProgramParameterName.ActiveUniforms, out var numOfUniforms);
			GL.GetProgram(Program, GetProgramParameterName.ActiveUniformMaxLength, out var numU);
			stringBuilder = new StringBuilder((numU == 0) ? 1 : numU);
			ActiveUniformType uniType;
			Console.WriteLine("start uni query");
			for (int i = 0; i < numOfUniforms; i++)
			{
				GL.GetActiveUniform(Program, i, stringBuilder.Capacity, out _, out var size, out uniType, out var name);
				Console.WriteLine(name + " " + uniType + " : " + size);
				var loc = GL.GetUniformLocation(Program, name);
				if (!uniformArray.ContainsKey(name))
					uniformArray.Add(name, loc);
				if (!attribArray.ContainsKey(loc))
					attribArray.Add(loc, (ConvertAttributeToParameterType(uniType), size));
			}
		}

		public void Draw(int indexStride, int start, int length)
		{
			var eleType = indexStride switch
			{
				2 => DrawElementsType.UnsignedShort,
				4 => DrawElementsType.UnsignedInt,
				_ => throw new NotSupportedException("Indexes other than ushort and uint are not supported")
			};
			GL.DrawElements(PrimitiveType.Triangles, length, eleType, (IntPtr)(start * indexStride));
			slot = 0;
		}
		public void Viewport(int x, int y, int width, int height)
		{
			GL.Viewport(x, y, width, height);
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
			GL.ClearDepth(1.0);
		}

		public void BindVertexAttrib(ParameterType type, int shaderLoc, int dim, int stride, int offset)
		{
			var vertAttribPtrType = type switch { 
				ParameterType.FLOAT => VertexAttribPointerType.Float,
				ParameterType.INT => VertexAttribPointerType.Int,
				ParameterType.VECTOR2 => VertexAttribPointerType.Float,
				ParameterType.VECTOR3 => VertexAttribPointerType.Float,
				ParameterType.VECTOR4 => VertexAttribPointerType.Float,
				ParameterType.COLOR3 => VertexAttribPointerType.Float,
				ParameterType.COLOR4 => VertexAttribPointerType.Float,
			};
			GL.VertexAttribPointer(shaderLoc, dim, vertAttribPtrType, false, stride, offset);
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

		public QueryScope StartQuery(Target target, int id)
		{
			var query = target switch
			{
				Target.OcclusionQuery => QueryTarget.SamplesPassed,
				_ => QueryTarget.SamplesPassed
			};
			GL.BeginQuery(query, id);
			return new QueryScope(this, target);
		}
		public void EndQuery(Target target)
		{
			switch (target)
			{
				case Target.OcclusionQuery: GL.EndQuery(QueryTarget.SamplesPassed); break;
			}
		}
		public void SetColorMask(bool r, bool g, bool b, bool a)
		{
			GL.ColorMask(r, g, b, a);
		}

		public void SetBlendState(BlendEquation srcColor, BlendEquation dstColor, BlendEquation srcAlpha, BlendEquation dstAlpha)
		{
			var srcC = srcColor switch
			{
				_ => BlendingFactorSrc.SrcAlpha,
			};
			var dstC = dstColor switch
			{
				_ => BlendingFactorDest.OneMinusSrcAlpha,
			};
			var srcA = dstColor switch
			{
				_ => BlendingFactorSrc.One,
			};
			var dstA = dstColor switch
			{
				_ => BlendingFactorDest.Zero,
			};
			GL.BlendFuncSeparate(srcC, dstC, srcA, dstA);
		}
		public void EnableState(RenderState state)
		{
			if (state.HasFlag(RenderState.Blend))
				GL.Enable(EnableCap.Blend);
			if (state.HasFlag(RenderState.DepthTest))
				GL.Enable(EnableCap.DepthTest);
			if (state.HasFlag(RenderState.Blend))
				GL.Enable(EnableCap.Blend);
			if (state.HasFlag(RenderState.DepthMask))
				GL.DepthMask(true);
			if (state.HasFlag(RenderState.Texture2D))
				GL.Enable(EnableCap.Texture2D);
			if (state.HasFlag(RenderState.ScissorTest))
				GL.Enable(EnableCap.ScissorTest);
			if (state.HasFlag(RenderState.CullBack | RenderState.CullFace))
			{
				GL.Enable(EnableCap.CullFace);
				GL.CullFace(CullFaceMode.FrontAndBack);
			}
			else if (state.HasFlag(RenderState.CullFace))
			{
				GL.Enable(EnableCap.CullFace);
				GL.CullFace(CullFaceMode.Front);
			}
			else if (state.HasFlag(RenderState.CullBack))
			{
				GL.Enable(EnableCap.CullFace);
				GL.CullFace(CullFaceMode.Back);
			}
		}
		public void DisableState(RenderState state)
		{
			if (state.HasFlag(RenderState.Blend))
				GL.Disable(EnableCap.Blend);
			if (state.HasFlag(RenderState.DepthTest))
				GL.Disable(EnableCap.DepthTest);
			if (state.HasFlag(RenderState.Blend))
				GL.Disable(EnableCap.Blend);
			if (state.HasFlag(RenderState.DepthMask))
				GL.DepthMask(false);
			if (state.HasFlag(RenderState.Texture2D))
				GL.Disable(EnableCap.Texture2D);
			if (state.HasFlag(RenderState.ScissorTest))
				GL.Disable(EnableCap.ScissorTest);
			if (state.HasFlag(RenderState.CullFace))
			{
				GL.Disable(EnableCap.CullFace);
			}
			else if (state.HasFlag(RenderState.CullBack))
			{
				GL.Disable(EnableCap.CullFace);
			}
		}
		public void SetDepthFunc(DepthFunc func)
		{
			var f = func switch
			{
				DepthFunc.Never => DepthFunction.Never,
				DepthFunc.Lequal => DepthFunction.Lequal,
				DepthFunc.Equal => DepthFunction.Equal,
				DepthFunc.NotEqual => DepthFunction.Notequal,
				DepthFunc.Gequal => DepthFunction.Gequal,
				DepthFunc.Greater => DepthFunction.Greater,
				DepthFunc.Always => DepthFunction.Always,
				_ => DepthFunction.Less
			};
			GL.DepthFunc(f);
		}

		public string GetName()
		{
			return "OpenGL";
		}

		public string GetVersion()
		{
			return "1.0";
		}

		public void ImportPlugins(Dictionary<string, object> plugins)
		{
			throw new NotImplementedException();
		} 
		 private ActiveUniformType ConvertParameterToAttributeType(ParameterType type)
		{
			return type switch
			{
				ParameterType.FLOAT => ActiveUniformType.Float,
				ParameterType.INT => ActiveUniformType.Int,
				ParameterType.VECTOR2 => ActiveUniformType.FloatVec2,
				ParameterType.VECTOR3 => ActiveUniformType.FloatVec3,
				ParameterType.VECTOR4 => ActiveUniformType.FloatVec4,
				ParameterType.COLOR3 => ActiveUniformType.FloatVec3,
				ParameterType.COLOR4 => ActiveUniformType.FloatVec4,
				ParameterType.MATRIX16 => ActiveUniformType.FloatMat4,
				ParameterType.FLOAT_ARRAY => ActiveUniformType.Float,
				ParameterType.INT_ARRAY => ActiveUniformType.Int,
				ParameterType.VECTOR2_ARRAY => ActiveUniformType.FloatVec2,
				ParameterType.VECTOR3_ARRAY => ActiveUniformType.FloatVec3,
				ParameterType.VECTOR4_ARRAY => ActiveUniformType.FloatVec4,
				ParameterType.COLOR3_ARRAY => ActiveUniformType.FloatVec3,
				ParameterType.COLOR4_ARRAY => ActiveUniformType.FloatVec4,
				ParameterType.MATRIX16_ARRAY => ActiveUniformType.FloatMat4,
				ParameterType.TEXTURE => ActiveUniformType.Sampler2D,
				ParameterType.MESH => 0,
				_ => 0
			};
		}

		private ParameterType ConvertAttributeToParameterType(ActiveUniformType type)
		{
			return type switch
			{
				ActiveUniformType.Int => ParameterType.INT,
				ActiveUniformType.UnsignedInt => throw new NotImplementedException(),
				ActiveUniformType.Float => ParameterType.FLOAT,
				ActiveUniformType.Double => throw new NotImplementedException(),
				ActiveUniformType.FloatVec2 => ParameterType.VECTOR2,
				ActiveUniformType.FloatVec3 => ParameterType.VECTOR3,
				ActiveUniformType.FloatVec4 => ParameterType.VECTOR4,
				ActiveUniformType.IntVec2 => throw new NotImplementedException(),
				ActiveUniformType.IntVec3 => throw new NotImplementedException(),
				ActiveUniformType.IntVec4 => throw new NotImplementedException(),
				ActiveUniformType.Bool => throw new NotImplementedException(),
				ActiveUniformType.BoolVec2 => throw new NotImplementedException(),
				ActiveUniformType.BoolVec3 => throw new NotImplementedException(),
				ActiveUniformType.BoolVec4 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat2 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat3 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat4 => ParameterType.MATRIX16,
				ActiveUniformType.Sampler1D => throw new NotImplementedException(),
				ActiveUniformType.Sampler2D => ParameterType.TEXTURE,
				ActiveUniformType.Sampler3D => throw new NotImplementedException(),
				ActiveUniformType.SamplerCube => throw new NotImplementedException(),
				ActiveUniformType.Sampler1DShadow => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DShadow => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DRect => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DRectShadow => throw new NotImplementedException(),
				ActiveUniformType.FloatMat2x3 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat2x4 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat3x2 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat3x4 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat4x2 => throw new NotImplementedException(),
				ActiveUniformType.FloatMat4x3 => throw new NotImplementedException(),
				ActiveUniformType.Sampler1DArray => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DArray => throw new NotImplementedException(),
				ActiveUniformType.SamplerBuffer => throw new NotImplementedException(),
				ActiveUniformType.Sampler1DArrayShadow => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DArrayShadow => throw new NotImplementedException(),
				ActiveUniformType.SamplerCubeShadow => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntVec2 => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntVec3 => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntVec4 => throw new NotImplementedException(),
				ActiveUniformType.IntSampler1D => throw new NotImplementedException(),
				ActiveUniformType.IntSampler2D => throw new NotImplementedException(),
				ActiveUniformType.IntSampler3D => throw new NotImplementedException(),
				ActiveUniformType.IntSamplerCube => throw new NotImplementedException(),
				ActiveUniformType.IntSampler2DRect => throw new NotImplementedException(),
				ActiveUniformType.IntSampler1DArray => throw new NotImplementedException(),
				ActiveUniformType.IntSampler2DArray => throw new NotImplementedException(),
				ActiveUniformType.IntSamplerBuffer => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler1D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler2D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler3D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSamplerCube => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler2DRect => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler1DArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler2DArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSamplerBuffer => throw new NotImplementedException(),
				ActiveUniformType.DoubleVec2 => throw new NotImplementedException(),
				ActiveUniformType.DoubleVec3 => throw new NotImplementedException(),
				ActiveUniformType.DoubleVec4 => throw new NotImplementedException(),
				ActiveUniformType.SamplerCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.SamplerCubeMapArrayShadow => throw new NotImplementedException(),
				ActiveUniformType.IntSamplerCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSamplerCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.Image1D => throw new NotImplementedException(),
				ActiveUniformType.Image2D => throw new NotImplementedException(),
				ActiveUniformType.Image3D => throw new NotImplementedException(),
				ActiveUniformType.Image2DRect => throw new NotImplementedException(),
				ActiveUniformType.ImageCube => throw new NotImplementedException(),
				ActiveUniformType.ImageBuffer => throw new NotImplementedException(),
				ActiveUniformType.Image1DArray => throw new NotImplementedException(),
				ActiveUniformType.Image2DArray => throw new NotImplementedException(),
				ActiveUniformType.ImageCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.Image2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.Image2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.IntImage1D => throw new NotImplementedException(),
				ActiveUniformType.IntImage2D => throw new NotImplementedException(),
				ActiveUniformType.IntImage3D => throw new NotImplementedException(),
				ActiveUniformType.IntImage2DRect => throw new NotImplementedException(),
				ActiveUniformType.IntImageCube => throw new NotImplementedException(),
				ActiveUniformType.IntImageBuffer => throw new NotImplementedException(),
				ActiveUniformType.IntImage1DArray => throw new NotImplementedException(),
				ActiveUniformType.IntImage2DArray => throw new NotImplementedException(),
				ActiveUniformType.IntImageCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.IntImage2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.IntImage2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage1D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage2D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage3D => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage2DRect => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImageCube => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImageBuffer => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage1DArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage2DArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImageCubeMapArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntImage2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.IntSampler2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler2DMultisample => throw new NotImplementedException(),
				ActiveUniformType.Sampler2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.IntSampler2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntSampler2DMultisampleArray => throw new NotImplementedException(),
				ActiveUniformType.UnsignedIntAtomicCounter => throw new NotImplementedException(),
				_ => 0
			};
		}

		T IBackendRenderer.ConvertParameterToAttributeType<T>(ParameterType type)
		{
			if (typeof(T) == typeof(ActiveUniformType))
				return (T)(object)ConvertParameterToAttributeType(type);
			return (T)(object)type;
		}

		ParameterType IBackendRenderer.ConvertAttributeToParameterType<T>(T type)
		{
			if (typeof(T) == typeof(ActiveUniformType))
				return ConvertAttributeToParameterType((ActiveUniformType)(object)type);
			return (ParameterType)(object)type;
		}
		#endregion IBackendRenderer implementation
	}
}