using SharpAsset;
using SharpAsset.Pipeline;
using System;
using Sharp.Engine.Components;
using SharpSL;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Sharp
{
	public class MeshRenderer : Renderer, IStartableComponent //where VertexFormat : struct, IVertex
	{
		public Curve[] curves = new Curve[2] {
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.1f, value = -10f }, new Keyframe() { time = 120f, value = 10f } } },
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.4f, value = 0f }, new Keyframe() { time = 60f, value = 1f } } }
		};
		public Material material;

		public MeshRenderer(Entity parent) : base(parent)
		{
		}

		public override void Render()
		{
			material.TryGetProperty("mesh", out Mesh Mesh);
			material.BindProperty("model", Parent.transform.ModelMatrix);
			//if (Camera.main.frustum.Intersect(Mesh.bounds, Parent.transform.ModelMatrix) == 0)
			{
				//Console.WriteLine("cull");
				//	return;
			}
			//Console.WriteLine ("no-cull ");

			material.SendData();
		}
		internal Material SwapMaterial(Material mat)
		{
			var prev = material;
			material = mat;
			prev.TryGetProperty("mesh", out Mesh Mesh);
			material.BindProperty("mesh", Mesh);
			return prev;
		}
		public void Start()
		{
			Console.WriteLine("start meshrenderer");
			//material.BindUnmanagedProperty("model", Parent.transform.ModelMatrix);
		}
		public override void Dispose()
		{
			base.Dispose();
			material.Dispose();
			curves.Dispose();//TODO: add Dispose to GUID
		}
	}
}