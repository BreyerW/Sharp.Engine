using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ImageSharpPlugin
{
	public static class TextureLoader
	{
		public static TextureData Import(string pathToFile)
		{
			using (var image = Image.Load<Bgra32>(pathToFile))
			{
				image.TryGetSinglePixelSpan(out var span);
				return new TextureData()
				{
					width = image.Width,
					height = image.Height,
					bitmap = MemoryMarshal.AsBytes(span).ToArray()
				};
			}
		}
	}
	public class TextureData
	{
		public int width;
		public int height;
		public byte[] bitmap;
	}
}
