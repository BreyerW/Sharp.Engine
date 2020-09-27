using System;
using System.IO;
using System.Runtime.InteropServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Advanced;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
	public class TexturePipeline : Pipeline<Texture>
	{
		public TexturePipeline()
		{
		}

		public override IAsset Import(string pathToFile)
		{
			var name = Path.GetFileNameWithoutExtension(pathToFile);
			if (nameToKey.Contains(name))
				return this[nameToKey.IndexOf(name)];
			var texture = new Texture();
			//Console.WriteLine (IsPowerOfTwo(texture.bitmap.Width) +" : "+IsPowerOfTwo(texture.bitmap.Height));
			//var format=ImageInfo.(pathToFile); 
			using (var image = Image.Load<Bgra32>(pathToFile))
			{
				texture.width = image.Width;
				texture.height = image.Height;
				texture.TBO = -1;
				texture.FBO = -1;
				texture.format = TextureFormat.RGBA;
				texture.FullPath = pathToFile;
				texture.bitmap = MemoryMarshal.AsBytes(image.GetPixelSpan()).ToArray();
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