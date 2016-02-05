using System;
using SharpAsset;
using Sharp.Editor.Views;
using System.Collections.Generic;
using OpenTK;

namespace SharpAsset
{
	public struct Material
	{
		internal static Dictionary<int, int> globalTexArray;
		internal static Dictionary<int, Matrix4> globalMat4Array;

		internal Dictionary<int, int> texArray;
		internal Dictionary<int, Matrix4> mat4Array;

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
			mat4Array = new Dictionary<int, Matrix4> ();
			globalTexArray = new Dictionary<int, int> ();
			globalMat4Array = new Dictionary<int, Matrix4> ();
		}
		public void SetProperty (string propName,ref Texture tex){
			if (!Shader.uniformArray.ContainsKey (UniformType.Sampler2D) || !Shader.uniformArray [UniformType.Sampler2D].ContainsKey (propName))
				return;

			SceneView.backendRenderer.GenerateBuffers(ref tex);
			SceneView.backendRenderer.BindBuffers (ref tex);
			SceneView.backendRenderer.Allocate(ref tex);
			texArray.Add (Shader.uniformArray [UniformType.Sampler2D][propName], tex.TBO);
		}
		public void SetProperty(string propName,ref Matrix4 mat){
			var shaderLoc = Shader.uniformArray [UniformType.FloatMat4] [propName];
			if (!mat4Array.ContainsKey (shaderLoc))
				mat4Array.Add (shaderLoc, mat);
			else
				mat4Array [shaderLoc] = mat;
		}

		public void SetGlobalProperty (string propName,ref Texture tex){
			if (!Shader.uniformArray.ContainsKey (UniformType.Sampler2D) || !Shader.uniformArray [UniformType.Sampler2D].ContainsKey (propName))
				return;

			SceneView.backendRenderer.GenerateBuffers(ref tex);
			SceneView.backendRenderer.BindBuffers (ref tex);
			SceneView.backendRenderer.Allocate(ref tex);
			globalTexArray.Add (Shader.uniformArray [UniformType.Sampler2D][propName], tex.TBO);
		}
		public void SetGlobalProperty(string propName,ref Matrix4 mat){
			//Console.WriteLine (propName);
			var shaderLoc = Shader.uniformArray [UniformType.FloatMat4] [propName];

			if (!globalMat4Array.ContainsKey (shaderLoc))
				globalMat4Array.Add (shaderLoc, mat);
			else
				globalMat4Array [shaderLoc] = mat;
		}
	}
}

