using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.Runtime.InteropServices;
using System.Numerics;

namespace ImageSharpPlugin
{
	public static class FontLoader
	{
		private static FontCollection loadedFonts = new();
		public static bool Import(string pathToFileOrSystemFont)
		{
			FontFamily fam = pathToFileOrSystemFont.Contains('\\') ? loadedFonts.Install(pathToFileOrSystemFont) : SystemFonts.Find(pathToFileOrSystemFont);
			return fam is not null;
		}
		public static (float, float) LoadMetrics(string fontName, float size, char c)
		{
			Font face = null;
			if (loadedFonts.TryFind(fontName, out var fam))
			{
				face = fam.CreateFont(size);
			}
			else
				face = SystemFonts.CreateFont(fontName, size);

			var glyph = face.GetGlyph(c);
			return (PixelToPointSize(glyph.Instance.LeftSideBearing, face.Size, face.EmSize), PixelToPointSize(glyph.Instance.AdvanceWidth, face.Size, face.EmSize));
		}
		public static (ushort,short, short) LoadFontData(string fontName, float size)
		{
			Font face = null;
			if (loadedFonts.TryFind(fontName, out var fam))
			{
				face = fam.CreateFont(size);
			}
			else
				face = SystemFonts.CreateFont(fontName, size);

			return (face.EmSize,face.Ascender, face.Descender);
		}
		public static Vector2 LoadKerning(string fontName, float size, char c, char next)
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
		public static (int, int, byte[]) GenerateTextureForChar(string fontName, float size, char c)
		{
			var fam = loadedFonts.Find(fontName);
			var face = fam.CreateFont(size);
			var nextPowerOfTwo = (int)MathF.Ceiling(MathF.Pow(2, (int)MathF.Log(size, 2) + 1));
			using Image<A8> img = new Image<A8>(Configuration.Default, nextPowerOfTwo, nextPowerOfTwo, new A8(0));
			img.Mutate(i => i.DrawText("" + c, face, Color.White, new PointF(0f, -3f)));
			img.TryGetSinglePixelSpan(out var span);
			return (img.Width, img.Height, MemoryMarshal.AsBytes(span).ToArray());
		}
		private static float PixelToPointSize(float px, float size, float emSize)
		{
			return (px * size) / (emSize);
		}
	}

}
