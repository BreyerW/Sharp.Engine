using SharpAsset;
using SharpAsset.Pipeline;
using System;
using Sharp.Engine.Components;
using SharpSL;
using System.Runtime.InteropServices;

namespace Sharp
{
	[Serializable]
	public class MeshRenderer : Renderer, IStartableComponent //where VertexFormat : struct, IVertex
	{
		public Material material
		{
			get;
			set;
		}
		public Curve[] curves { get; set; } = new Curve[2] {
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.1f, value = -10f }, new Keyframe() { time = 120f, value = 10f } } },
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.4f, value = 0f }, new Keyframe() { time = 60f, value = 1f } } }
		};
		public MeshRenderer(Entity parent) : base(parent)
		{
			var shader = (Shader)Pipeline.Get<Shader>().Import(@"B:\Sharp.Engine3\Sharp.Core\bin\Debug\netcoreapp3.1\Content\TextureOnlyShader.shader");
			material = new Material();
			material.Shader = shader;
		}

		public override void Render()
		{
			material.TryGetProperty("mesh",out Mesh Mesh);
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

			material.InternalSetProperty("model", Parent.transform.ModelMatrix);

			material.SendData();
			MainWindow.backendRenderer.ChangeShader();
		}

		public void Start()
		{
			Console.WriteLine("start meshrenderer");
			//material.BindUnmanagedProperty("model", Parent.transform.ModelMatrix);
		}
	}
}