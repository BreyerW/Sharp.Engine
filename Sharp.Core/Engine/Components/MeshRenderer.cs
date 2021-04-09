﻿using SharpAsset;
using System;
using Sharp.Engine.Components;
using Sharp.Core.Engine;
using System.Runtime.CompilerServices;
using SharpAsset.AssetPipeline;
using System.Text.Json.Serialization;

namespace Sharp
{
	public class MeshRenderer : Renderer, IStartableComponent //where VertexFormat : struct, IVertex
	{
		[JsonInclude]
		private int physicIndex = -1;

		public Curve[] curves = new Curve[2] {
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.1f, value = -10f }, new Keyframe() { time = 120f, value = 10f } } },
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.4f, value = 0f }, new Keyframe() { time = 60f, value = 1f } } }
		};

		public MeshRenderer(Entity parent) : base(parent)
		{
			parent.transform.onTransformChanged += OnTransformChange;
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
		public ref Mesh Mesh => ref material.GetPropertyByRef("mesh");
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