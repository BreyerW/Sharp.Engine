using Sharp.Editor.Views;
using SharpAsset;
using System;
using System.Text.Json.Serialization;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		[JsonInclude]
		public Material material;

		internal Material SwapMaterial(Material mat)
		{
			var prev = material;
			material = mat;
			prev.TryGetProperty(Material.MESHSLOT, out Mesh Mesh);
			material.BindProperty(Material.MESHSLOT, Mesh);
			return prev;
		}
		protected Renderer(Entity parent) : base(parent)
		{
		}

		public abstract void Render();

		internal override void OnActiveChanged()
		{
		}
	}
}