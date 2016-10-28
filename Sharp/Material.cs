using System;
using SharpAsset;
using Sharp.Editor.Views;
using System.Collections.Generic;
using OpenTK;

namespace SharpAsset
{
	public struct Material
	{
		internal static Dictionary<string, int> globalTexArray = new Dictionary<string, int>();
		internal static Dictionary<string, Matrix4> globalMat4Array = new Dictionary<string, Matrix4>();

		internal Dictionary<int, int> texArray;
		internal Dictionary<int, Matrix4> mat4Array;

		private int shaderId;

		public Shader Shader{
			get{
                foreach (var shader in Shader.shaders.Values) {
					if (shader.Program == shaderId)
						return shader;
				}
				throw new IndexOutOfRangeException("Material dont point to any shader");
			}
            set {
                shaderId = value.Program;
                if (texArray == null || mat4Array == null)
                {
                    mat4Array = new Dictionary<int, Matrix4>();
                    texArray = new Dictionary<int, int>();
                }
            }
		}
		public void SetProperty (string propName,ref Texture tex){
			if (!Shader.uniformArray.ContainsKey (UniformType.Sampler2D) || !Shader.uniformArray [UniformType.Sampler2D].ContainsKey (propName))
				return;
			texArray.Add (Shader.uniformArray [UniformType.Sampler2D][propName], tex.TBO);
		}
		public void SetProperty(string propName,ref Matrix4 mat){
            var shaderLoc = Shader.uniformArray [UniformType.FloatMat4] [propName];
			if (!mat4Array.ContainsKey (shaderLoc))
				mat4Array.Add (shaderLoc, mat);
			else
				mat4Array [shaderLoc] = mat;
		}

		public static void SetGlobalProperty (string propName,ref Texture tex){
			
            if (globalTexArray.ContainsKey(propName))
                globalTexArray[propName] = tex.TBO;
            else
                globalTexArray.Add(propName, tex.TBO);
		}
		public static void SetGlobalProperty(string propName,ref Matrix4 mat){
            if (globalMat4Array.ContainsKey(propName))
                globalMat4Array[propName] = mat;
            else
                globalMat4Array.Add(propName, mat);
        }
	}
}

