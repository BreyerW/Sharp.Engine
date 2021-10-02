﻿//MIT, 2014-present, WinterDev
//-----------------------------------
using PixelFarm.Drawing.Fonts;
using System.Collections.Generic;
using Typography.TextLayout;

namespace PixelFarm.Drawing.Text
{
    public class ManagedShapingService : TextShapingService
    {
        protected override void GetGlyphPosImpl(ActualFont actualFont, char[] buffer, int startAt, int len, List<UnscaledGlyphPlan> properGlyphs)
        {
            //do shaping and set text layout
        }
    }
}