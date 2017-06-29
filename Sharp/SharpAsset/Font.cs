using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using OpenTK;
using SharpFont;
using Sharp;
using HB = SharpFont.HarfBuzz;

namespace SharpAsset
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Font : IAsset
    {
        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }

        public Face face;
        public float Size { get => (float)face.Size.Metrics.Height; set => face.SetCharSize(0, value, 72, 72); }

        public void PlaceIntoScene(Entity context, Vector3 worldPos)
        {
            throw new NotImplementedException();
        }

        public (int width, int height) Measure(string text)
        {
            face.SetCharSize(0, 50, 72, 72);

            var font = HB.Font.FromFTFace(face);
            var buf = new HB.Buffer();
            buf.Direction = HB.Direction.RightToLeft;
            buf.Script = HB.Script.Arabic;
            //buf.AddText("متن");
            buf.AddText(text);
            HB.HB.Shape(font, buf);

            var glyphInfos = buf.GlyphInfo();
            var glyphPositions = buf.GlyphPositions();

            int height = (face.MaxAdvanceHeight - face.Descender) >> 6;
            int width = 0;
            for (int i = 0; i < glyphInfos.Length; ++i)
            {
                width += glyphPositions[i].xAdvance >> 6;
            }
            return (width, height);
        }
    }
}

/* float width = 0, height = 0;
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
 for (var i = 0; i < chars.Length; i++)
 {
     // Look up the glyph index for this character.
     uint glyphIndex = face.GetCharIndex(chars[i]);

     // Load the glyph into the font's glyph slot. There is usually only one slot in the font.
     face.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

     float gAdvanceX = (float)face.Glyph.Advance.X; // same as the advance in metrics
     float gBearingX = (float)face.Glyph.Metrics.HorizontalBearingX;
     float gWidth = face.Glyph.Metrics.Width.ToSingle();
     underrun += -(gBearingX);
     if (width == 0)
         width += underrun;
     // Accumulate overrun, which coould cause clipping at the right side of characters near
     // the end of the string (typically affects fonts with slanted characters)
     if (gBearingX + gWidth > 0 || gAdvanceX > 0)
     {
         overrun -= Math.Max(gBearingX + gWidth, gAdvanceX);
         if (overrun <= 0) overrun = 0;
     }
     overrun += (float)(gBearingX == 0 && gWidth == 0 ? 0 : gBearingX + gWidth - gAdvanceX);
     // On the last character, apply whatever overrun we have to the overall width.
     // Positive overrun prevents clipping, negative overrun prevents extra space.
     if (i == chars.Length - 1)
         width += overrun;
     // If this character goes higher or lower than any previous character, adjust
     // the overall height of the bitmap.
     float glyphTop = (float)face.Glyph.Metrics.HorizontalBearingY;
     float glyphBottom = (float)(face.Glyph.Metrics.Height - face.Glyph.Metrics.HorizontalBearingY);
     if (glyphTop > top)
         top = glyphTop;
     if (glyphBottom > bottom)
         bottom = glyphBottom;
     // Accumulate the distance between the origin of each character (simple width).
     width += gAdvanceX;
     // Calculate kern for the NEXT character (if any)
     // The kern value adjusts the origin of the next character (positive or negative).
     if (face.HasKerning && i < chars.Length - 1)
     {
         char cNext = chars[i + 1];
         kern = (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Default).X;
         // sanity check for some fonts that have kern way out of whack
         if (kern > gAdvanceX * 5 || kern < -(gAdvanceX * 5))
             kern = 0;
         width += kern;
     }
 }
 height = top + bottom;*/