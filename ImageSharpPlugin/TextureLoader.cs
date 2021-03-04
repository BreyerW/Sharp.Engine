using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using PluginAbstraction;

namespace ImageSharpPlugin
{
	public class TextureLoader : ITextureLoaderPlugin
	{
		public TextureData Import(string pathToFile)
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
		public string GetName()
		{
			return "TextureLoader";
		}

		public string GetVersion()
		{
			return "1.0";
		}

		public void ImportPlugins(Dictionary<string, object> plugins)
		{
			throw new System.NotImplementedException();
		}
	}

}
