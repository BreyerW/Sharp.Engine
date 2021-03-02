using Sharp.Core;
using System;
using System.Numerics;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".ttf", ".otf")]
	internal class FontPipeline : Pipeline<Font>
	{
		public static Func<string, bool> import;
		public static Func<string, float, char, char, Vector2> loadKerning;
		public static Func<string, float, char, (float, float)> loadMetrics;
		public static Func<string, float, (ushort, short, short)> loadFontData;
		public static Func<string, float, char, (int width, int height, byte[] pixels)> generateTexture;
		public static float size = 18;
		static FontPipeline()
		{
			import = PluginManager.ImportAPI<Func<string, bool>>("FontLoader", "Import");
			loadKerning = PluginManager.ImportAPI<Func<string, float, char, char, Vector2>>("FontLoader", "LoadKerning");
			loadMetrics = PluginManager.ImportAPI<Func<string, float, char, (float, float)>>("FontLoader", "LoadMetrics");
			loadFontData = PluginManager.ImportAPI<Func<string, float, (ushort, short, short)>>("FontLoader", "LoadFontData");
			generateTexture = PluginManager.ImportAPI<Func<string, float, char, (int, int, byte[])>>("FontLoader", "GenerateTextureForChar");
		}

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}

		public override IAsset Import(string pathToFileOrSystemFont)
		{
			if (base.Import(pathToFileOrSystemFont) is IAsset asset) return asset;
			if (import(pathToFileOrSystemFont) is false) return null;
			var font = new Font(size);
			font.FullPath = pathToFileOrSystemFont;
			var (emSize, asc, desc) = loadFontData(font.Name.ToString(), size);
			font.EmSize = emSize;
			font.Ascender = asc;
			font.Descender = desc;
			return this[Register(font)];
		}
	}
}