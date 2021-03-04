using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginAbstraction
{
	public interface ITextureLoaderPlugin : IPlugin
	{
		public TextureData Import(string pathToFile);
	}
	public class TextureData
	{
		public int width;
		public int height;
		public byte[] bitmap;
	}
}
