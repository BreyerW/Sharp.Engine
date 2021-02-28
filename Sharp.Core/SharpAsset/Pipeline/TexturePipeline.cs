using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sharp.Core;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
	public class TexturePipeline : Pipeline<Texture>
	{
		private static Func<string, IEnumerable<(string, int, byte[])>> import;
		static TexturePipeline()
		{
			import = PluginManager.ImportAPI<Func<string, IEnumerable<(string, int, byte[])>>>("TextureLoader", "Import");
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
			foreach (var (attribName, stride, data) in import(pathToFile))
			{
				if (attribName is "dimensions")
				{
					texture.width = Unsafe.ReadUnaligned<int>(ref data[0]);
					texture.height = Unsafe.ReadUnaligned<int>(ref data[stride]);
				}
				else if (attribName is "data")
				{
					texture.bitmap = GC.AllocateUninitializedArray<byte>(data.Length);
					Unsafe.CopyBlockUnaligned(ref texture.bitmap[0], ref data[0], (uint)data.Length);
				}
			}
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
}