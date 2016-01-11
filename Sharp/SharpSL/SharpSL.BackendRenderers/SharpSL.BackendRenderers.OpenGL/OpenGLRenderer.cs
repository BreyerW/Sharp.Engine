using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Drawing;
using SharpAsset;
using System.Runtime.InteropServices;

namespace SharpSL.BackendRenderers.OpenGL
{
	public class OpenGLRenderer: IBackendRenderer
	{
		#region IBackendRenderer implementation

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
		public void Allocate<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible{
			Console.WriteLine ("huuu");
			//if (IsLoaded) return;
			//VBO
			//int tmpVBO;
			//GL.GenBuffers(1, out tmpVBO);
			//Console.WriteLine ("error check"+GL.DebugMessageCallback);

			var watch =System.Diagnostics.Stopwatch.StartNew();
			GL.BindBuffer (BufferTarget.ArrayBuffer, mesh.VBO);
			var stride = Marshal.SizeOf (mesh.Vertices [0]);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(mesh.Vertices.Length * stride),CustomConverter.ToByteArray(mesh.Vertices,stride),(BufferUsageHint)mesh.UsageHint);

			watch.Stop();
			Console.WriteLine("cast: "+ watch.ElapsedTicks);
			//int tmpEBO;
			//GL.GenBuffers(1, out tmpEBO);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer,mesh.EBO);
			GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(mesh.Indices.Length * Marshal.SizeOf(mesh.Indices[0])),ref mesh.Indices[0],(BufferUsageHint)mesh.UsageHint);

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
		public void Allocate (ref Shader shader)
		{
			int status_code;
			string info;

			shader.VertexID = GL.CreateShader(ShaderType.VertexShader);
			shader.FragmentID = GL.CreateShader(ShaderType.FragmentShader);

			// Compile vertex shader
			GL.ShaderSource(shader.VertexID,shader.VertexSource);
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

			shader.Program = GL.CreateProgram();
			GL.AttachShader(shader.Program,shader.FragmentID);
			GL.AttachShader(shader.Program, shader.VertexID);

			GL.BindAttribLocation(shader.Program, 0, "vertex_position");
			GL.BindAttribLocation(shader.Program, 1, "vertex_color");
			GL.BindAttribLocation(shader.Program, 2, "vertex_texcoord");
			GL.BindAttribLocation(shader.Program, 3, "vertex_normal");

			GL.LinkProgram(shader.Program);
			//GL.UseProgram(Program);

			//Console.WriteLine (attribType);
			//GL.UseProgram(0);
		}
		public void Use<IndexType> (ref Mesh<IndexType> mesh) where IndexType: struct, IConvertible
		{
			GL.DrawElements (PrimitiveType.Triangles, mesh.Indices.Length, (DrawElementsType)Mesh<IndexType>.indiceType, IntPtr.Zero);
		}
		public void Delete (ref Shader shader)
		{
			if (shader.Program != 0)
				GL.DeleteProgram(shader.Program);
			if (shader.FragmentID != 0)
				GL.DeleteShader(shader.FragmentID);
			if (shader.VertexID != 0)
				GL.DeleteShader(shader.VertexID);
		}
		public void Scissor (int x, int y, int width, int height)
		{
			GL.Scissor (x, y, width, height);
			GL.Viewport (x, y, width,height);
		}

		public void ClearBuffer ()
		{
			GL.Clear(ClearBufferMask.DepthBufferBit|ClearBufferMask.ColorBufferBit);
		}

		public void SetupGraphic ()
		{
			GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);
			GL.Hint(HintTarget.LineSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
			GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
		}

		public void GenerateBuffers (out int ebo, out int vbo)
		{
			ebo=GL.GenBuffer();
			vbo=GL.GenBuffer();
		}

		public void BindBuffers (int ebo,int vbo)
		{
			GL.BindBuffer (BufferTarget.ArrayBuffer,vbo);
			GL.BindBuffer (BufferTarget.ElementArrayBuffer,ebo);
		}

		public void ChangeShader (int program, ref Matrix4 mvp)
		{
			GL.UseProgram (program);
			int mvp_matrix_location = GL.GetUniformLocation (program, "mvp_matrix");
			GL.UniformMatrix4 (mvp_matrix_location, false, ref mvp);
		}
		public void ChangeShader ()
		{
			GL.UseProgram (0);
		}
		public void BindVertexAttrib(int program,int stride, RegisterAsAttribute attrib){
			var loc = GL.GetAttribLocation (program, attrib.shaderLocationName);
			if (loc != -1)
				GL.EnableVertexAttribArray(loc);
			GL.VertexAttribPointer(loc,attrib.Dimension, attrib.type, false,stride, attrib.offset);
		}
		#endregion
	}
}

