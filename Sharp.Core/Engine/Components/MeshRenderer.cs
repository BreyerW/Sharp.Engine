using Sharp.Core;
using Sharp.Engine.Components;
using Sharp.Physic;
using SharpAsset;
using SharpAsset.AssetPipeline;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Sharp
{
	public partial class MeshRenderer : Renderer, IStartableComponent //where VertexFormat : struct, IVertex
	{
		[ModuleInitializer]
		public static void Register()
		{
			ref var mask = ref StaticDictionary<Renderer>.Get<BitMask>();
			if (mask.IsDefault)
				mask = new BitMask(0);
			mask.SetFlag(Extension.RegisterComponent<MeshRenderer>());
		}
		[JsonInclude]
		private int physicIndex = -1;

		public Curve[] curves = new Curve[2] {
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.1f, value = -10f }, new Keyframe() { time = 120f, value = 10f } } },
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.4f, value = 0f }, new Keyframe() { time = 60f, value = 1f } } }
		};
		protected override void Initialize()
		{
			Parent.transform.onTransformChanged += OnTransformChange;
		}
		public override void Render()
		{
			material.BindProperty("model", Parent.transform.ModelMatrix);
			material.Draw();
		}
		public void SaveMeshChanges()
		{
			ref var mesh = ref Mesh;
			if (CollisionDetection.frozenMapping.ContainsKey(Parent.GetInstanceID()))
				CollisionDetection.UpdateFrozenBody(Parent.GetInstanceID());
			else
				physicIndex = CollisionDetection.AddFrozenBody(Parent.GetInstanceID(), Parent.transform.Position, mesh.bounds.Min, mesh.bounds.Max, physicIndex);
		}
		public ref Mesh Mesh => ref material.GetProperty(Material.MESHSLOT);
		public void Start()
		{
			Console.WriteLine("start meshrenderer");
			ref var mesh = ref Mesh;
			if (CollisionDetection.frozenMapping.ContainsKey(Parent.GetInstanceID()))
				CollisionDetection.UpdateFrozenBody(Parent.GetInstanceID());
			else
				physicIndex = CollisionDetection.AddFrozenBody(Parent.GetInstanceID(), Parent.transform.Position, mesh.bounds.Min, mesh.bounds.Max, physicIndex);

			//material.BindUnmanagedProperty("model", Parent.transform.ModelMatrix);
		}
		private void OnTransformChange()
		{
			CollisionDetection.UpdateFrozenBody(Parent.GetInstanceID());
		}
		public override void Dispose()
		{
			base.Dispose();
			Parent.transform.onTransformChanged -= OnTransformChange;
			CollisionDetection.RemoveFrozenBody(Parent.GetInstanceID());
			material.Dispose();
			curves.Dispose();//TODO: add Dispose to GUID
		}
	}
}