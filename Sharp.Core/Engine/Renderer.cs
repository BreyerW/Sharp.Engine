using Sharp.Core;
using Sharp.Editor.Views;
using SharpAsset;
using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ref var mask = ref StaticDictionary<Renderer>.Get<BitMask>();
			if (mask.IsDefault)
				mask = new BitMask(0);
		}
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


		public abstract void Render();

		internal override void OnActiveChanged()
		{
		}
	}
}