//MIT, 2019-present, WinterDev

using PixelFarm.CpuBlit;
using PixelFarm.Drawing;
using System;
using System.Collections.Generic;

namespace PixelFarm.PathReconstruction
{
    public class MixedRegion : CpuBlitRegion
    {
        internal MixedRegion()
        {

        }
        public override bool IsSimpleRect => false; //TEMP!
        public override CpuBlitRegionKind Kind => CpuBlitRegionKind.MixedRegion;

        public override Region CreateComplement(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

        public override Region CreateExclude(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

        public override Region CreateIntersect(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }

        public override Region CreateUnion(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }
        public override Region CreateXor(Region another)
        {
            CpuBlitRegion rgnB = another as CpuBlitRegion;
            if (rgnB == null) return null;
            //

            return null;
        }
        public override Rectangle GetRectBounds()
        {
            throw new System.NotSupportedException();
        }
        public override bool IsVisible(PointF p)
        {
            throw new NotImplementedException();
        }
        public override bool IsVisible(RectangleF p)
        {
            throw new NotImplementedException();
        }
    }
}