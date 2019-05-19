using SharpAsset;
using SharpAsset.Pipeline;
using System;

namespace Sharp
{
	[Serializable]
	public class MeshRenderer : Renderer //where VertexFormat : struct, IVertex
	{
		private Mesh mesh;
		public ref Mesh Mesh
		{
			get
			{
				return ref mesh;
			}
		}

		public Material material;

		public MeshRenderer(Entity parent) : base(parent)
		{
			var shader = (Shader)Pipeline.Get<ShaderPipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\TextureOnlyShader.shader");
			//Pipeline.Pipeline.GetPipeline<ShaderPipeline>().GetAsset("TextureOnlyShader");
			material = new Material();
			material.Shader = shader;
		}

		public override void Render()
		{
			if (Camera.main.frustum.Intersect(Mesh.bounds, Parent.transform.ModelMatrix) == 0)
			{
				//Console.WriteLine("cull");
				return;
			}
			//Console.WriteLine ("no-cull ");

			//int current = GL.GetInteger (GetPName.CurrentProgram);
			//GL.ValidateProgram (material.shaderId);
			//if (current != material.shaderId) {
			//}
			//if (!IsLoaded) return;
			var shader = material.Shader;

			MainWindow.backendRenderer.Use(shader.Program);

			//entityObject.SetModelMatrix();
			material.InternalSetProperty("model", Parent.transform.ModelMatrix);
			material.SendData();
			MainWindow.backendRenderer.Use(ref Mesh.indiceType, Mesh.Indices.Length);
			MainWindow.backendRenderer.ChangeShader();
		}
	}
}