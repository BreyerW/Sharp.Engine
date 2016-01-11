using System;
using FreeImageAPI;
using System.IO;

namespace SharpAsset.Pipeline
{
	[SupportedFileFormats(".bmp",".jpg",".png")]
	public class TexturePipeline:Pipeline
	{
		public static readonly TexturePipeline singleton=new TexturePipeline();

		public TexturePipeline ()
		{
		}
		public override IAsset Import (string pathToFile)
		{
			var name=Path.GetFileNameWithoutExtension (pathToFile);
			if (Texture.textures.ContainsKey (name))
				return Texture.textures[name];
			var texture = new Texture ();
			var dib=FreeImage.LoadEx (pathToFile);
			texture.texId = -1;
			texture.FullPath = pathToFile;
			texture.bitmap=FreeImage.GetBitmap (dib);
			//Console.WriteLine (IsPowerOfTwo(texture.bitmap.Width) +" : "+IsPowerOfTwo(texture.bitmap.Height));
			Texture.textures.Add(texture.Name,texture);
			return texture;
		}
		public static bool IsPowerOfTwo(int x)
		{
			return (x != 0) && ((x & (x - 1)) == 0);
		}
		public override void Export (string pathToExport, string format)
		{
			throw new NotImplementedException ();
		}
	}
}

