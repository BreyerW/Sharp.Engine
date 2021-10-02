//MIT, 2016-present, WinterDev

using PixelFarm.CpuBlit.VertexProcessing;
using System;

namespace PixelFarm.CpuBlit
{
    public static class AggPainterPool
    {
        public static TempContext<AggPainter> Borrow(MemBitmap bmp, out AggPainter painter)
        {

            if (!Temp<AggPainter>.IsInit())
            {
                Temp<AggPainter>.SetNewHandler(
                    () => new AggPainter(new AggRenderSurface()),
                    p =>
                    {
                        p.RenderSurface.DetachDstBitmap();
                        p.Reset();
                    }
                    );
            }

            var tmpPainter = Temp<AggPainter>.Borrow(out painter);
            painter.RenderSurface.AttachDstBitmap(bmp);
            return tmpPainter;
        }
    }

}

