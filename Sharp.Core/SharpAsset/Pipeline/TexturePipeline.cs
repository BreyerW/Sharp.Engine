using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Core;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
	public class TexturePipeline : Pipeline<Texture>
	{
		private static Func<string, object> import;
		static TexturePipeline()
		{
			import = PluginManager.ImportAPI<Func<string, object>>("TextureLoader", "Import");
		}

		public override IAsset Import(string pathToFile)
		{
			if (base.Import(pathToFile) is IAsset asset) return asset;
			var texture = new Texture();
			texture.TBO = -1;
			texture.FBO = -1;
			texture.format = TextureFormat.RGBA;
			texture.FullPath = pathToFile;
			//Console.WriteLine (IsPowerOfTwo(texture.bitmap.Width) +" : "+IsPowerOfTwo(texture.bitmap.Height));
			//var format=ImageInfo.(pathToFile); 
			var data = import(pathToFile);
			var texData = Unsafe.As<TextureData>(data);
			texture.width = texData.width;
			texture.height = texData.height;
			texture.bitmap = GC.AllocateUninitializedArray<byte>(texData.bitmap.Length);
			Unsafe.CopyBlockUnaligned(ref texture.bitmap[0], ref texData.bitmap[0], (uint)texData.bitmap.Length);

			return this[Register(texture)];
		}

		private static bool IsPowerOfTwo(int x)
		{
			return (x != 0) && ((x & (x - 1)) == 0);
		}

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}
	}
	public class TextureData
	{
		public int width;
		public int height;
		public byte[] bitmap;
	}
}