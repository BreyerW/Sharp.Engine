using System;
using SharpAsset;
using Sharp.Editor.Views;
using System.Collections.Generic;
using OpenTK;

namespace SharpAsset
{
	public struct Material
	{
		internal Dictionary<int, int> texArray;
		internal Dictionary<int, Matrix4> matrix4Array;

		internal int shaderId;

		public Shader Shader{
			get{
				foreach (var shader in Shader.shaders.Values) {
					if (shader.Program == shaderId)
						return shader;
				}
				throw new IndexOutOfRangeException("Material dont point to any shader");
			}
		}
		public Material(int program){
			shaderId = program;
			texArray = new Dictionary<int, int> ();
			matrix4Array = new Dictionary<int, Matrix4> ();
		}
		public void SetShaderProperty (string propName,ref Texture tex){
			if (!Shader.uniformArray.ContainsKey (UniformType.Sampler2D) || !Shader.uniformArray [UniformType.Sampler2D].ContainsKey (propName))
				return;

			SceneView.backendRenderer.GenerateBuffers(ref tex);
			SceneView.backendRenderer.BindBuffers (ref tex);
			SceneView.backendRenderer.Allocate(ref tex);
			texArray.Add (Shader.uniformArray [UniformType.Sampler2D][propName], tex.TBO);
		}
		public void SetShaderProperty(string propName,ref Matrix4 mat){
			var shaderLoc = Shader.uniformArray [UniformType.FloatMat4] [propName];
			if (!matrix4Array.ContainsKey (shaderLoc))
				matrix4Array.Add (shaderLoc, mat);
			else
				matrix4Array [shaderLoc] = mat;
		}
	}
}

