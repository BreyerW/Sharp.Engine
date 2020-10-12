using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Numerics;
using Sharp;

namespace SharpAsset
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Font : IAsset
	{
		public string FullPath { get; set; }
		public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }

		//public Texture atlas;
		public Dictionary<uint, (Texture tex, float bearing, float advance)> metrics;
		public int nominalHeight;
		//public int Size { get => face.Size.Metrics.NominalHeight; set => face.SetCharSize(0, value, 0, 0); }

		public void PlaceIntoScene(Entity context, Vector3 worldPos)
		{
			throw new NotImplementedException();
		}

		/* public (int width, int height) Measure(string text, int position = -1)
		 {

		   /*  if (string.IsNullOrEmpty(text)) return (0, 0);

			 var chars = text.AsSpan();
			 float width = 0, height = 0;
			 float overrun = 0;
			 float underrun = 0;
			 float kern = 0;
			 // Bottom and top are both positive for simplicity.
			 // Drawing in .Net has 0,0 at the top left corner, with positive X to the right
			 // and positive Y downward.
			 // Glyph metrics have an origin typically on the left side and at baseline
			 // of the visual data, but can draw parts of the glyph in any quadrant, and
			 // even move the origin (via kerning).
			 float top = 0, bottom = 0;
			 var length = chars.Length;
			 if (position is 0)
				 length = 1;
			 else if (position > 0)
				 length = position;
			 var metrics = face.nominalHeight;
			 (float x, float y) scale = (metrics.Metrics.ScaleX.ToSingle(), metrics.Metrics.ScaleY.ToSingle());
			 metrics.Dispose();
			 for (var i = 0; i < length; i++)
			 {
				 // Look up the glyph index for this character.
				 uint glyphIndex = face.GetCharIndex(chars[i]);

				 // Load the glyph into the font's glyph slot. There is usually only one slot in the font.
				 //if (!fontAtlas.ContainsKey(chars[i]))
				   //  GenerateBitmapForChar(chars[i]);

				 var texChar = fontAtlas[chars[i]];
				 // underrun += gBearingX;
				 //if (width == 0)
				 //  width += underrun;
				 /*if (underrun <= 0)
				 {
					 underrun = 0;
				 }*

				 // Accumulate overrun, which coould cause clipping at the right side of characters near
				 // the end of the string (typically affects fonts with slanted characters)
				 //if (gBearingX + gWidth > 0 || gAdvanceX > 0)
				 {
					 //   overrun -= Math.Max(gBearingX + gWidth, gAdvanceX);
					 //  if (overrun <= 0) overrun = 0;
				 }
				 //overrun += (float)(gBearingX == 0 && gWidth == 0 ? 0 : gBearingX + gWidth - gAdvanceX);
				 // On the last character, apply whatever overrun we have to the overall width.
				 // Positive overrun prevents clipping, negative overrun prevents extra space.

				 // If this character goes higher or lower than any previous character, adjust
				 // the overall height of the bitmap.

				 float glyphTop = -texChar.bearing.y * scale.y;
				 float glyphBottom = texChar.texture.height - texChar.bearing.y * scale.y;
				 if (glyphTop > top)
					 top = glyphTop;
				 if (glyphBottom > bottom)
					 bottom = glyphBottom;

				 if (position is 0)
					 break;

				 // Accumulate the distance between the origin of each character (simple width).
				 //if (i == chars.Length - 1)
				 //width += overrun;
				 width += texChar.advance.x + 2;
				 // Calculate kern for the NEXT character (if any)
				 // The kern value adjusts the origin of the next character (positive or negative).
				 if (face.HasKerning && i < chars.Length - 1)
				 {
					 char cNext = chars[i + 1];
					 kern = (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
					 // sanity check for some fonts that have kern way out of whack
					 if (kern > texChar.advance.x * 5 || kern < -(texChar.advance.x * 5))
						 kern = 0;
					 width += kern;
				 }
			 }

			 height = top + bottom;

			 return ((int)(width * scale.x), (int)(height * scale.y));*
		 }*/

		/* public void GenerateBitmapForChar(uint c)
		 {
			 uint charCode = face.GetCharIndex(c);
			 face.LoadChar(c, LoadFlags.NoScale, LoadTarget.Normal);

			 if (c != ' ')
			 {
				 face.Glyph.RenderGlyph(RenderMode.Normal);
				 var bitmap = face.Glyph.Bitmap;
				 var cBmp = bitmap.BufferData;

				 var width = NumericsExtensions.NextPowerOfTwo(bitmap.Width);
				 var height = NumericsExtensions.NextPowerOfTwo(bitmap.Rows);
				 int tbo = -1;
				 MainWindow.backendRenderer.GenerateBuffers(ref tbo);
				 MainWindow.backendRenderer.BindBuffers(ref tbo);//check all binds they may cause unnecessary memory consumption

				 fontAtlas.Add(c, (new Texture() { bitmap = new byte[(int)(width * height)], width = (int)width, height = (int)height, TBO = tbo }, (face.Glyph.Metrics.HorizontalBearingX.ToInt32(), face.Glyph.Metrics.HorizontalBearingY.ToInt32()), (face.Glyph.Advance.X.ToInt32(), face.Glyph.Advance.Y.ToInt32())));//change bearing to vertical if vertical layout
				 for (int j = 0; j < bitmap.Rows; j++)
					 Unsafe.CopyBlock(ref fontAtlas[c].texture.bitmap[(int)(j * width)], ref cBmp[j * bitmap.Width], (uint)bitmap.Width);
			 }
			 else fontAtlas.Add(c, (new Texture() { bitmap = Array.Empty<byte>(), width = 0, height = 0 }, (face.Glyph.Metrics.HorizontalBearingX.ToInt32(), face.Glyph.Metrics.HorizontalBearingY.ToInt32()), (face.Glyph.Advance.X.ToInt32(), face.Glyph.Advance.Y.ToInt32())));//change bearing to vertical if vertical layout
		 }*/
	}
}

/* */
