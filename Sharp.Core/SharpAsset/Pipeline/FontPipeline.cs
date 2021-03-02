using Sharp.Core;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".ttf", ".otf")]
	internal class FontPipeline : Pipeline<Font>
	{
		public static Func<string, bool> import;
		public static Func<string, float, char, char, Vector2> loadKerning;
		public static Func<string, float, char, (float, float)> loadMetrics;
		public static Func<string, float, object> loadFontData;
		public static Func<string, float, char,object> generateTexture;
		public static float size = 18;
		static FontPipeline()
		{
			import = PluginManager.ImportAPI<Func<string, bool>>("FontLoader", "Import");
			loadKerning = PluginManager.ImportAPI<Func<string, float, char, char, Vector2>>("FontLoader", "LoadKerning");
			loadMetrics = PluginManager.ImportAPI<Func<string, float, char, (float, float)>>("FontLoader", "LoadMetrics");
			loadFontData = PluginManager.ImportAPI<Func<string, float, object>>("FontLoader", "LoadFontData");
			generateTexture = PluginManager.ImportAPI<Func<string, float, char, object>>("FontLoader", "GenerateTextureForChar");
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
			var fData = loadFontData(font.Name.ToString(), size);
			var fontData = Unsafe.As<FontData>(fData);
			font.EmSize = fontData.emSize;
			font.Ascender = fontData.ascender;
			font.Descender = fontData.descender;
			return this[Register(font)];
		}
	}
	public class FontData
	{
		public ushort emSize;
		public short ascender;
		public short descender;
	}
}