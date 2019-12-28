﻿/////////////////////////////////////////////////////////////////////////////////
// Paint.NET (MIT,from version 3.36.7, see=> https://github.com/rivy/OpenPDN   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////
//MIT, 2017-present, WinterDev
using PixelFarm.Drawing;
namespace PaintFx.Effects
{

    public class OutlineRenderer : HistogramRenderer
    {
        private int thickness;
        private int intensity;
        public int Thickness
        {
            get { return thickness; }
            set { thickness = value; }

        }
        public int Intensity
        {
            get { return intensity; }
            set { intensity = value; }
        }

        public unsafe override ColorBgra Apply(ColorBgra src, int area, int* hb, int* hg, int* hr, int* ha)
        {
            int minCount1 = area * (100 - this.intensity) / 200;
            int minCount2 = area * (100 + this.intensity) / 200;

            int bCount = 0;
            int b1 = 0;
            while (b1 < 255 && hb[b1] == 0)
            {
                ++b1;
            }

            while (b1 < 255 && bCount < minCount1)
            {
                bCount += hb[b1];
                ++b1;
            }

            int b2 = b1;
            while (b2 < 255 && bCount < minCount2)
            {
                bCount += hb[b2];
                ++b2;
            }

            int gCount = 0;
            int g1 = 0;
            while (g1 < 255 && hg[g1] == 0)
            {
                ++g1;
            }

            while (g1 < 255 && gCount < minCount1)
            {
                gCount += hg[g1];
                ++g1;
            }

            int g2 = g1;
            while (g2 < 255 && gCount < minCount2)
            {
                gCount += hg[g2];
                ++g2;
            }

            int rCount = 0;
            int r1 = 0;
            while (r1 < 255 && hr[r1] == 0)
            {
                ++r1;
            }

            while (r1 < 255 && rCount < minCount1)
            {
                rCount += hr[r1];
                ++r1;
            }

            int r2 = r1;
            while (r2 < 255 && rCount < minCount2)
            {
                rCount += hr[r2];
                ++r2;
            }

            int aCount = 0;
            int a1 = 0;
            while (a1 < 255 && hb[a1] == 0)
            {
                ++a1;
            }

            while (a1 < 255 && aCount < minCount1)
            {
                aCount += ha[a1];
                ++a1;
            }

            int a2 = a1;
            while (a2 < 255 && aCount < minCount2)
            {
                aCount += ha[a2];
                ++a2;
            }

            return ColorBgra.FromBgra(
                (byte)(255 - (b2 - b1)),
                (byte)(255 - (g2 - g1)),
                (byte)(255 - (r2 - r1)),
                (byte)(a2));
        }
        public override void Render(Surface src, Surface dest, Rectangle[] renderRects, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                RenderRect(this.thickness, src, dest, renderRects[i]);
            }
        }
    }
}