using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PluginAbstraction
{
	public interface IFontLoaderPlugin : IPlugin
	{
		public bool Import(string pathToFileOrSystemFont);
		public (float bearing, float advance) LoadMetrics(string fontName, float size, char c);
		public FontData LoadFontData(string fontName, float size);
		public Vector2 LoadKerning(string fontName, float size, char c, char next);
		public TextureData GenerateTextureForChar(string fontName, float size, char c);
	}
	public class FontData
	{
		public ushort emSize;
		public short ascender;
		public short descender;
	}
}
