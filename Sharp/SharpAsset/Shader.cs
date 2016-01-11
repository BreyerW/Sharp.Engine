using System;
using System.IO;
using System.Collections.Generic;

namespace SharpAsset
{
	//This will hold our shader code in a nice clean class
	//this example only uses a shader with position and color
	//but didnt want to leave out the other bits for the shader
	//so you could practice writing a shader on your own :P
	public struct Shader: IAsset
	{
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		public string VertexSource { get; private set; }
		public string FragmentSource { get; private set; }

		public int VertexID;
		public int FragmentID;

		public int Program;

		public static Dictionary<string,Shader> shaders=new Dictionary<string, Shader>();

		public Shader(ref string vs, ref string fs, ref string pathToFile)
		{
			VertexSource = vs;
			FragmentSource = fs;
			FullPath=pathToFile;
			FragmentID = 0;
			VertexID = 0;
			Program = 0;
			shaders.Add (Name,this);
		}
		public override string ToString ()
		{
			return Name;
		}
		internal void Compile()
		{
			//SceneView.backendRenderer.CompileShader (out Program, out FragmentID, out VertexID, FragmentSource, VertexSource);
		}

		public void Dispose()
		{
			//SceneView.backendRenderer.DeleteShader (Program,FragmentID,VertexID);
		}
	}

}

