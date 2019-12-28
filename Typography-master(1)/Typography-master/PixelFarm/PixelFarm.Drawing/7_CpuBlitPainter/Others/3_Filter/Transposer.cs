//BSD, 2014-present, WinterDev
//----------------------------------------------------------------------------
// Anti-Grain Geometry - Version 2.4
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
//
// C# Port port by: Lars Brubaker
//                  larsbrubaker@gmail.com
// Copyright (C) 2007
//
// Permission to copy, use, modify, sell and distribute this software 
// is granted provided this copyright notice appears in all copies. 
// This software is provided "as is" without express or implied
// warranty, and with no claim as to its suitability for any purpose.
//
//----------------------------------------------------------------------------
// Contact: mcseem@antigrain.com
//          mcseemagg@yahoo.com
//          http://www.antigrain.com
//----------------------------------------------------------------------------

using PixelFarm.Drawing;
namespace PixelFarm.CpuBlit.Imaging
{
    //=======================================================pixfmt_transposer
    sealed class FormatTransposer : ProxyImage
    {
        public FormatTransposer(PixelProcessing.IBitmapBlender pixelFormat)
            : base(pixelFormat)
        {
        }

        public override int Width => _linkedImage.Height; //*** TRANSPOSE
        public override int Height => _linkedImage.Width; //*** TRANSPOSE

        public override Color GetPixel(int x, int y)
        {
            return _linkedImage.GetPixel(y, x);
        }


        public override void CopyHL(int x, int y, int len, Color c)
        {
            _linkedImage.CopyVL(y, x, len, c);
        }


        public override void CopyVL(int x, int y,
                                   int len,
                                   Color c)
        {
            _linkedImage.CopyHL(y, x, len, c);
        }

        public override void BlendHL(int x1, int y, int x2, Color c, byte cover)
        {
            _linkedImage.BlendVL(y, x1, x2, c, cover);
        }

        public override void BlendVL(int x, int y1, int y2, Color c, byte cover)
        {
            _linkedImage.BlendHL(y1, x, y2, c, cover);
        }

        public override void BlendSolidHSpan(int x, int y, int len, Color c, byte[] covers, int coversIndex)
        {
            _linkedImage.BlendSolidVSpan(y, x, len, c, covers, coversIndex);
        }

        public override void BlendSolidVSpan(int x, int y, int len, Color c, byte[] covers, int coversIndex)
        {
            _linkedImage.BlendSolidHSpan(y, x, len, c, covers, coversIndex);
        }

        public override void CopyColorHSpan(int x, int y, int len, Color[] colors, int colorsIndex)
        {
            _linkedImage.CopyColorVSpan(y, x, len, colors, colorsIndex);
        }

        public override void CopyColorVSpan(int x, int y, int len, Color[] colors, int colorsIndex)
        {
            _linkedImage.CopyColorHSpan(y, x, len, colors, colorsIndex);
        }

        public override void BlendColorHSpan(int x, int y, int len, Color[] colors, int colorsIndex, byte[] covers, int coversIndex, bool firstCoverForAll)
        {
            _linkedImage.BlendColorVSpan(y, x, len, colors, colorsIndex, covers, coversIndex, firstCoverForAll);
        }

        public override void BlendColorVSpan(int x, int y, int len, Color[] colors, int colorsIndex, byte[] covers, int coversIndex, bool firstCoverForAll)
        {
            _linkedImage.BlendColorHSpan(y, x, len, colors, colorsIndex, covers, coversIndex, firstCoverForAll);
        }
    }
}
