using Sharp.Core;
using System;
using System.Numerics;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".ttf", ".otf")]
	internal class FontPipeline : Pipeline<Font>
	{
		public static float size = 18;

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}

		public override IAsset Import(string pathToFileOrSystemFont)
		{
			if (base.Import(pathToFileOrSystemFont) is IAsset asset) return asset;
			if (PluginManager.fontLoader.Import(pathToFileOrSystemFont) is false) return null;
			var font = new Font(size);
			font.FullPath = pathToFileOrSystemFont;
			var data = PluginManager.fontLoader.LoadFontData(font.Name.ToString(), size);
			font.EmSize = data.emSize;
			font.Ascender = data.ascender;
			font.Descender = data.descender;
			return this[Register(font)];
		}
	}
}