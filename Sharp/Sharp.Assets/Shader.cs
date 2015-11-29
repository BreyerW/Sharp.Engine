using System;
using System.IO;
using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;

namespace Sharp
{
	//This will hold our shader code in a nice clean class
	//this example only uses a shader with position and color
	//but didnt want to leave out the other bits for the shader
	//so you could practice writing a shader on your own :P
	public class Shader: IAsset
	{
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		public string VertexSource { get; private set; }
		public string FragmentSource { get; private set; }

		public int VertexID { get; private set; }
		public int FragmentID { get; private set; }

		public int Program { get; private set; }

		public static Dictionary<string,Shader> shaders=new Dictionary<string, Shader>();

		public Shader(ref string vs, ref string fs, ref string pathToFile)
		{
			VertexSource = vs;
			FragmentSource = fs;
			FullPath=pathToFile;
			Build();
			shaders.Add (Name,this);
		}
		public override string ToString ()
		{
			return Name;
		}
		private void Build()
		{
			int status_code;
			string info;

			VertexID = GL.CreateShader(ShaderType.VertexShader);
			FragmentID = GL.CreateShader(ShaderType.FragmentShader);

			// Compile vertex shader
			GL.ShaderSource(VertexID, VertexSource);
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

			Program = GL.CreateProgram();
			GL.AttachShader(Program, FragmentID);
			GL.AttachShader(Program, VertexID);

			GL.BindAttribLocation(Program, 0, "vertex_position");
			GL.BindAttribLocation(Program, 1, "vertex_color");
			GL.BindAttribLocation (Program, 2, "vertex_texcoord");
			GL.BindAttribLocation(Program, 3, "vertex_normal");

			GL.LinkProgram(Program);
			//GL.UseProgram(Program);

			//Console.WriteLine (attribType);
			//GL.UseProgram(0);
		}

		public void Dispose()
		{
			if (Program != 0)
				GL.DeleteProgram(Program);
			if (FragmentID != 0)
				GL.DeleteShader(FragmentID);
			if (VertexID != 0)
				GL.DeleteShader(VertexID);
		}
	}

}

