using PluginAbstraction;
using Sharp.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset.AssetPipeline
{
	[SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
	public class TexturePipeline : Pipeline<Texture>
	{
		//private Dictionary<int, TextureRole> renderTextures = new();

		[ModuleInitializer]
		internal static void LoadPipeline()
		{
			allPipelines.Add(typeof(TexturePipeline).BaseType, instance);
			extensionToTypeMapping.Add(".bmp", typeof(TexturePipeline).BaseType);
			extensionToTypeMapping.Add(".jpg", typeof(TexturePipeline).BaseType);
			extensionToTypeMapping.Add(".png", typeof(TexturePipeline).BaseType);
			extensionToTypeMapping.Add(".dds", typeof(TexturePipeline).BaseType);
		}
		public static readonly TexturePipeline instance = new();
		protected override ref Texture ImportInternal(string pathToFile)
		{
			ref var asset = ref base.ImportInternal(pathToFile);
			if (Unsafe.IsNullRef(ref asset) is false) return ref asset;

			var texture = new Texture();

			texture.format = TextureFormat.RGBA;
			texture.FullPath = pathToFile;
			//var format=ImageInfo.(pathToFile); 
			var data = PluginManager.textureLoader.Import(pathToFile);

			texture.width = data.width;
			texture.height = data.height;
			texture.data = GC.AllocateUninitializedArray<byte>(data.bitmap.Length + Unsafe.SizeOf<int>(), true);
			Unsafe.CopyBlockUnaligned(ref texture.data[0], ref data.bitmap[0], (uint)data.bitmap.Length);
			texture.TBO = -1;
			Console.WriteLine(BitOperations.IsPow2(texture.width) + " : " + BitOperations.IsPow2(texture.height));

			return ref this[Register(texture)];
		}
		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}
		/*public int RegisterRenderTexture(in Texture asset, TextureRole role)
		{
			var id = Register(asset);
			//renderTextures.Add(id, role);
			return id;
		}*/
		public override void ApplyAsset(in Texture asset, object context)
		{
			throw new NotImplementedException();
		}

		public override void GenerateGraphicDeviceId()
		{
			Span<int> id = stackalloc int[1];
			while (recentlyLoadedAssets.TryDequeue(out var i))
			{
				ref var tex = ref GetAsset(i);
				PluginManager.backendRenderer.GenerateBuffers(Target.Texture, id);
				tex.TBO = id[0];
				PluginManager.backendRenderer.BindBuffers(Target.Texture, tex.TBO);
				PluginManager.backendRenderer.Allocate(ref tex.data is null ? ref Unsafe.NullRef<byte>() : ref tex.data[0], tex.width, tex.height, tex.format);

			}
		}
	}
}