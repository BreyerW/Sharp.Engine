using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Runtime.InteropServices;
using System.Numerics;
using PluginAbstraction;
using System.Collections.Generic;

namespace ImageSharpPlugin
{
	public class FontLoader : IFontLoaderPlugin
	{
		private static FontCollection loadedFonts = new();
		public bool Import(string pathToFileOrSystemFont)
		{
			FontFamily fam = pathToFileOrSystemFont.Contains('\\') ? loadedFonts.Install(pathToFileOrSystemFont) : SystemFonts.Find(pathToFileOrSystemFont);
			return fam is not null;
		}
		public (float, float) LoadMetrics(string fontName, float size, char c)
		{
			Font face = null;
			if (loadedFonts.TryFind(fontName, out var fam))
			{
				face = fam.CreateFont(size);
			}
			else
				face = SystemFonts.CreateFont(fontName, size);

			var glyph = face.GetGlyph(c);
			return (glyph.Instance.LeftSideBearing, glyph.Instance.AdvanceWidth);
		}
		public FontData LoadFontData(string fontName, float size)
		{
			Font face = null;
			if (loadedFonts.TryFind(fontName, out var fam))
			{
				face = fam.CreateFont(size);
			}
			else
				face = SystemFonts.CreateFont(fontName, size);

			return new FontData() { emSize = face.EmSize, ascender = face.Ascender, descender = face.Descender };
		}
		public Vector2 LoadKerning(string fontName, float size, char c, char next)
		{
			Font face = null;
			if (loadedFonts.TryFind(fontName, out var fam))
			{
				face = fam.CreateFont(size);
			}
			else
				face = SystemFonts.CreateFont(fontName, size);

			var glyph = face.GetGlyph(c).Instance;
			var nextGlyph = face.GetGlyph(next).Instance;
			return face.Instance.GetOffset(glyph, nextGlyph);
		}
		public TextureData GenerateTextureForChar(string fontName, float size, char c)
		{
			var fam = loadedFonts.Find(fontName);
			var face = fam.CreateFont(size);
			var nextPowerOfTwo = (int)MathF.Ceiling(MathF.Pow(2, (int)MathF.Log(size, 2) + 1));
			using Image<A8> img = new Image<A8>(Configuration.Default, nextPowerOfTwo, nextPowerOfTwo, new A8(0));
			img.Mutate(i => i.DrawText("" + c, face, Color.White, new PointF(0f, -3f)));
			img.TryGetSinglePixelSpan(out var span);
			return new TextureData()
			{
				width = img.Width,
				height = img.Height,
				bitmap = MemoryMarshal.AsBytes(span).ToArray()
			};
		}
		public string GetName()
		{
			return "FontLoader";
		}

		public string GetVersion()
		{
			return "1.0";
		}
		public void ImportPlugins(Dictionary<string, object> plugins)
		{
			throw new NotImplementedException();
		}
	}
}
