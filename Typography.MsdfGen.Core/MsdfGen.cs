//MIT, 2016, Viktor Chlumsky, Multi-channel signed distance field generator, from https://github.com/Chlumsky/msdfgen
//MIT, 2017-present, WinterDev (C# port)

using System;
using System.Collections.Generic;
using Typography.OpenFont;
using static Msdfgen.MsdfGenerator;

namespace Msdfgen
{
    public static class SdfGenerator
    {
        //siged distance field generator

        public static void GenerateSdf(FloatBmp output,
          Shape shape,
          double range,
          Vector2 scale,
          Vector2 translate)
        {

            List<Contour> contours = shape.contours;
            int contourCount = contours.Count;
            int w = output.Width, h = output.Height;
            List<int> windings = new List<int>(contourCount);
            for (int i = 0; i < contourCount; ++i)
            {
                windings.Add(contours[i].winding());
            }

            //# ifdef MSDFGEN_USE_OPENMP
            //#pragma omp parallel
            //#endif
            {

                //# ifdef MSDFGEN_USE_OPENMP
                //#pragma omp for
                //#endif
                double[] contourSD = new double[contourCount];
                for (int y = 0; y < h; ++y)
                {
                    int row = shape.InverseYAxis ? h - y - 1 : y;
                    for (int x = 0; x < w; ++x)
                    {
                        double dummy = 0;
                        Vector2 p = (new Vector2(x + .5, y + .5) / scale) - translate;
                        double negDist = -SignedDistance.INFINITE.distance;
                        double posDist = SignedDistance.INFINITE.distance;
                        int winding = 0;


                        for (int i = 0; i < contourCount; ++i)
                        {
                            Contour contour = contours[i];
                            SignedDistance minDistance = SignedDistance.INFINITE;
                            List<EdgeHolder> edges = contour.edges;
                            int edgeCount = edges.Count;
                            for (int ee = 0; ee < edgeCount; ++ee)
                            {
                                EdgeHolder edge = edges[ee];
                                SignedDistance distance = edge.edgeSegment.signedDistance(p, out dummy);
                                if (distance < minDistance)
                                    minDistance = distance;
                            }

                            contourSD[i] = minDistance.distance;
                            if (windings[i] > 0 && minDistance.distance >= 0 && Math.Abs(minDistance.distance) < Math.Abs(posDist))
                                posDist = minDistance.distance;
                            if (windings[i] < 0 && minDistance.distance <= 0 && Math.Abs(minDistance.distance) < Math.Abs(negDist))
                                negDist = minDistance.distance;
                        }

                        double sd = SignedDistance.INFINITE.distance;
                        if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                        {
                            sd = posDist;
                            winding = 1;
                            for (int i = 0; i < contourCount; ++i)
                                if (windings[i] > 0 && contourSD[i] > sd && Math.Abs(contourSD[i]) < Math.Abs(negDist))
                                    sd = contourSD[i];
                        }
                        else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                        {
                            sd = negDist;
                            winding = -1;
                            for (int i = 0; i < contourCount; ++i)
                                if (windings[i] < 0 && contourSD[i] < sd && Math.Abs(contourSD[i]) < Math.Abs(posDist))
                                    sd = contourSD[i];
                        }
                        for (int i = 0; i < contourCount; ++i)
                            if (windings[i] != winding && Math.Abs(contourSD[i]) < Math.Abs(sd))
                                sd = contourSD[i];

                        output.SetPixel(x, row, (float)(sd / range + .5));
                    }
                }
            }



        }
        public static void GenerateSdf_legacy(FloatBmp output,
            Shape shape,
            double range,
            Vector2 scale,
            Vector2 translate)
        {

            int w = output.Width;
            int h = output.Height;
            for (int y = 0; y < h; ++y)
            {
                int row = shape.InverseYAxis ? h - y - 1 : y;
                for (int x = 0; x < w; ++x)
                {
                    double dummy = 0;
                    Vector2 p = (new Vector2(x + 0.5f, y + 0.5) * scale) - translate;
                    SignedDistance minDistance = SignedDistance.INFINITE;
                    //TODO: review here
                    List<Contour> contours = shape.contours;
                    int m = contours.Count;
                    for (int n = 0; n < m; ++n)
                    {
                        Contour contour = contours[n];
                        List<EdgeHolder> edges = contour.edges;
                        int nn = edges.Count;
                        for (int i = 0; i < nn; ++i)
                        {
                            EdgeHolder edge = edges[i];
                            SignedDistance distance = edge.edgeSegment.signedDistance(p, out dummy);
                            if (distance < minDistance)
                            {
                                minDistance = distance;
                            }
                        }
                    }
                    output.SetPixel(x, row, (float)(minDistance.distance / (range + 0.5f)));
                }
            }
        }

    }


    public static class MsdfGenerator
    {
        //multi-channel signed distance field generator
        struct EdgePoint
        {
            public SignedDistance minDistance;
            public EdgeHolder nearEdge;
            public double nearParam;
        }
        struct MultiDistance
        {
            public double r, g, b;
            public double med;
        }
        public class ContourBuilder : IGlyphTranslator
        {

            List<Contour> _contours;
            float _curX;
            float _curY;
            float _latestMoveToX;
            float _latestMoveToY;
            Contour _currentCnt;
            EdgeSegment _latestPart;
            //
            public ContourBuilder()
            {

            }
            public List<Contour> GetContours() => _contours;
            public void MoveTo(float x0, float y0)
            {
                _latestMoveToX = _curX = x0;
                _latestMoveToY = _curY = y0;
                _latestPart = null;
                //----------------------------

            }
            public void LineTo(float x1, float y1)
            {
                if (_latestPart != null)
                {
                    _currentCnt.AddEdge(_latestPart = new LinearSegment(_latestPart.p[0], new Vector2(x1, y1)));
                }
                else
                {
                    _currentCnt.AddEdge(_latestPart = new LinearSegment(new Vector2(_curX, _curY), new Vector2(x1, y1)));
                }
                _curX = x1;
                _curY = y1;
            }


            public void Curve3(float x1, float y1, float x2, float y2)
            {
                if (_latestPart != null)
                {
                    _currentCnt.AddEdge(_latestPart = new QuadraticSegment(
                     _latestPart.p[0],
                     new Vector2(x1, y1),
                     new Vector2(x2, y2)));
                }
                else
                {
                    _currentCnt.AddEdge(new QuadraticSegment(
                         new Vector2(_curX, _curY),
                         new Vector2(x1, y1),
                         new Vector2(x2, y2)));
                }

                _curX = x2;
                _curY = y2;
            }
            public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                throw new NotSupportedException();
                /*	if (_latestPart != null)
					{
						_currentCnt.AddEdge(_latestPart = new CubicSegment(
						   _latestPart,
							x1, y1,
							x2, y2,
							x3, y3));
					}
					else
					{
						_currentCnt.AddEdge(_latestPart = new CubicSegment(
						   _curX, _curY,
						   x1, y1,
						   x2, y2,
						   x3, y3));
					}
					_curX = x3;
					_curY = y3;*/
            }

            public void CloseContour()
            {
                if (_curX == _latestMoveToX && _curY == _latestMoveToY)
                {
                    //we not need to close 
                }
                else
                {
                    if (_latestPart != null)
                    {
                        _currentCnt.AddEdge(_latestPart = new LinearSegment(_latestPart.p[0], new Vector2(_latestMoveToX, _latestMoveToY)));
                    }
                    else
                    {
                        _currentCnt.AddEdge(_latestPart = new LinearSegment(new Vector2(_curX, _curY), new Vector2(_latestMoveToX, _latestMoveToY)));
                    }
                }

                _curX = _latestMoveToX;
                _curY = _latestMoveToY;

                if (_currentCnt != null &&
                    _currentCnt.edges.Count > 0)
                {
                    _contours.Add(_currentCnt);
                    _currentCnt = null;
                }
                //
                _currentCnt = new Contour();
            }
            public void BeginRead(int contourCount)
            {
                //reset all
                _contours = new List<Contour>();
                _latestPart = null;
                _latestMoveToX = _curX = _latestMoveToY = _curY = 0;
                //
                _currentCnt = new Contour();
                //new contour, but not add
            }
            public void EndRead()
            {

            }

        }
        static Vector2 GetMidPoint(Vector2 v1, Vector2 v2)
        {
            return new Vector2((float)((v1.x + v2.x) / 2f), (float)((v1.y + v2.y) / 2f));
        }
        static float median(float a, float b, float c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        static double median(double a, double b, double c)
        {
            return Math.Max(Math.Min(a, b), Math.Min(Math.Max(a, b), c));
        }
        static bool pixelClash(FloatRGB a, FloatRGB b, double threshold)
        {
            // Only consider pair where both are on the inside or both are on the outside
            bool aIn = ((a.r > 0.5f) ? 1 : 0) + ((a.g > .5f) ? 1 : 0) + ((a.b > .5f) ? 1 : 0) >= 2;
            bool bIn = ((b.r > 0.5f) ? 1 : 0) + ((b.g > .5f) ? 1 : 0) + ((b.b > .5f) ? 1 : 0) >= 2;

            if (aIn != bIn) return false;
            // If the change is 0 <-> 1 or 2 <-> 3 channels and not 1 <-> 1 or 2 <-> 2, it is not a clash
            if ((a.r > .5f && a.g > .5f && a.b > .5f) || (a.r < .5f && a.g < .5f && a.b < .5f)
                || (b.r > .5f && b.g > .5f && b.b > .5f) || (b.r < .5f && b.g < .5f && b.b < .5f))
                return false;
            // Find which color is which: _a, _b = the changing channels, _c = the remaining one
            float aa, ab, ba, bb, ac, bc;
            if ((a.r > .5f) != (b.r > .5f) && (a.r < .5f) != (b.r < .5f))
            {
                aa = a.r; ba = b.r;
                if ((a.g > .5f) != (b.g > .5f) && (a.g < .5f) != (b.g < .5f))
                {
                    ab = a.g; bb = b.g;
                    ac = a.b; bc = b.b;
                }
                else if ((a.b > .5f) != (b.b > .5f) && (a.b < .5f) != (b.b < .5f))
                {
                    ab = a.b; bb = b.b;
                    ac = a.g; bc = b.g;
                }
                else
                    return false; // this should never happen
            }
            else if ((a.g > .5f) != (b.g > .5f) && (a.g < .5f) != (b.g < .5f)
              && (a.b > .5f) != (b.b > .5f) && (a.b < .5f) != (b.b < .5f))
            {
                aa = a.g; ba = b.g;
                ab = a.b; bb = b.b;
                ac = a.r; bc = b.r;
            }
            else
                return false;
            // Find if the channels are in fact discontinuous
            return (Math.Abs(aa - ba) >= threshold)
                && (Math.Abs(ab - bb) >= threshold)
                && Math.Abs(ac - .5f) >= Math.Abs(bc - .5f); // Out of the pair, only flag the pixel farther from a shape edge
        }
        static void msdfErrorCorrection(FloatRGBBmp output, Vector2 threshold)
        {
            //Pair<int,int> is List<Point>
            List<Pair<int, int>> clashes = new List<Pair<int, int>>();
            int w = output.Width, h = output.Height;
            for (int y = 0; y < h; ++y)
            {
                for (int x = 0; x < w; ++x)
                {
                    if ((x > 0 && pixelClash(output.GetPixel(x, y), output.GetPixel(x - 1, y), threshold.x))
                        || (x < w - 1 && pixelClash(output.GetPixel(x, y), output.GetPixel(x + 1, y), threshold.x))
                        || (y > 0 && pixelClash(output.GetPixel(x, y), output.GetPixel(x, y - 1), threshold.y))
                        || (y < h - 1 && pixelClash(output.GetPixel(x, y), output.GetPixel(x, y + 1), threshold.y)))
                    {
                        clashes.Add(new Pair<int, int>(x, y));
                    }
                }
            }
            int clash_count = clashes.Count;
            for (int i = 0; i < clash_count; ++i)
            {
                Pair<int, int> clash = clashes[i];
                FloatRGB pixel = output.GetPixel(clash.first, clash.second);
                float med = median(pixel.r, pixel.g, pixel.b);
                //set new value back
                output.SetPixel(clash.first, clash.second, new FloatRGB(med, med, med));
            }
            //for (std::vector<std::pair<int, int>>::const_iterator clash = clashes.begin(); clash != clashes.end(); ++clash)
            //{
            //    FloatRGB & pixel = output(clash->first, clash->second);
            //    float med = median(pixel.r, pixel.g, pixel.b);
            //    pixel.r = med, pixel.g = med, pixel.b = med;
            //}
        }

        public static int[] ConvertToIntBmp(FloatRGBBmp input)
        {
            int height = input.Height;
            int width = input.Width;
            int[] output = new int[input.Width * input.Height];

            for (int y = height - 1; y >= 0; --y)
            {
                for (int x = 0; x < width; ++x)
                {
                    //a b g r
                    //----------------------------------
                    FloatRGB pixel = input.GetPixel(x, y);
                    //a b g r
                    //for big-endian color
                    //int abgr = (255 << 24) |
                    //    Vector2.Clamp((int)(pixel.r * 0x100), 0xff) |
                    //    Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                    //    Vector2.Clamp((int)(pixel.b * 0x100), 0xff) << 16;

                    //for little-endian color

                    int abgr = (255 << 24) |
                        Vector2.Clamp((int)(pixel.r * 0x100), 0xff) << 16 |
                        Vector2.Clamp((int)(pixel.g * 0x100), 0xff) << 8 |
                        Vector2.Clamp((int)(pixel.b * 0x100), 0xff);

                    output[(y * width) + x] = abgr;
                    //----------------------------------
                    /**it++ = clamp(int(bitmap(x, y).r*0x100), 0xff);
                    *it++ = clamp(int(bitmap(x, y).g*0x100), 0xff);
                    *it++ = clamp(int(bitmap(x, y).b*0x100), 0xff);*/
                }
            }
            return output;
        }


        public static void generateMSDF(FloatRGBBmp output, Shape shape, double range, Vector2 scale, Vector2 translate, double edgeThreshold)
        {
            List<Contour> contours = shape.contours;
            int contourCount = contours.Count;
            int w = output.Width;
            int h = output.Height;
            List<int> windings = new List<int>(contourCount);
            for (int i = 0; i < contourCount; ++i)
            {
                windings.Add(contours[i].winding());
            }

            var contourSD = new MultiDistance[contourCount];

            for (int y = 0; y < h; ++y)
            {
                int row = shape.InverseYAxis ? h - y - 1 : y;
                for (int x = 0; x < w; ++x)
                {
                    Vector2 p = (new Vector2(x + .5, y + .5) / scale) - translate;
                    EdgePoint sr = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        sg = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        sb = new EdgePoint { minDistance = SignedDistance.INFINITE };
                    double d = Math.Abs(SignedDistance.INFINITE.distance);
                    double negDist = -Math.Abs(SignedDistance.INFINITE.distance);
                    double posDist = Math.Abs(SignedDistance.INFINITE.distance);
                    int winding = 0;

                    for (int n = 0; n < contourCount; ++n)
                    {
                        //for-each contour
                        Contour contour = contours[n];
                        List<EdgeHolder> edges = contour.edges;
                        int edgeCount = edges.Count;
                        EdgePoint r = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        g = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        b = new EdgePoint { minDistance = SignedDistance.INFINITE };
                        for (int ee = 0; ee < edgeCount; ++ee)
                        {
                            EdgeHolder edge = edges[ee];
                            double param;
                            SignedDistance distance = edge.edgeSegment.signedDistance(p, out param);
                            if (edge.HasComponent(EdgeColor.RED) && distance < r.minDistance)
                            {
                                r.minDistance = distance;
                                r.nearEdge = edge;
                                r.nearParam = param;
                            }
                            if (edge.HasComponent(EdgeColor.GREEN) && distance < g.minDistance)
                            {
                                g.minDistance = distance;
                                g.nearEdge = edge;
                                g.nearParam = param;
                            }
                            if (edge.HasComponent(EdgeColor.BLUE) && distance < b.minDistance)
                            {
                                b.minDistance = distance;
                                b.nearEdge = edge;
                                b.nearParam = param;
                            }
                        }
                        //----------------
                        if (r.minDistance < sr.minDistance)
                            sr = r;
                        if (g.minDistance < sg.minDistance)
                            sg = g;
                        if (b.minDistance < sb.minDistance)
                            sb = b;
                        //----------------
                        double medMinDistance = Math.Abs(median(r.minDistance.distance, g.minDistance.distance, b.minDistance.distance));
                        if (medMinDistance < d)
                        {
                            d = medMinDistance;
                            winding = -windings[n];
                        }

                        if (r.nearEdge != null)
                            r.nearEdge.edgeSegment.distanceToPseudoDistance(ref r.minDistance, p, r.nearParam);
                        if (g.nearEdge != null)
                            g.nearEdge.edgeSegment.distanceToPseudoDistance(ref g.minDistance, p, g.nearParam);
                        if (b.nearEdge != null)
                            b.nearEdge.edgeSegment.distanceToPseudoDistance(ref b.minDistance, p, b.nearParam);
                        //--------------
                        medMinDistance = median(r.minDistance.distance, g.minDistance.distance, b.minDistance.distance);
                        contourSD[n].r = r.minDistance.distance;
                        contourSD[n].g = g.minDistance.distance;
                        contourSD[n].b = b.minDistance.distance;
                        contourSD[n].med = medMinDistance;
                        if (windings[n] > 0 && medMinDistance >= 0 && Math.Abs(medMinDistance) < Math.Abs(posDist))
                            posDist = medMinDistance;
                        if (windings[n] < 0 && medMinDistance <= 0 && Math.Abs(medMinDistance) < Math.Abs(negDist))
                            negDist = medMinDistance;
                    }
                    if (sr.nearEdge != null)
                        sr.nearEdge.edgeSegment.distanceToPseudoDistance(ref sr.minDistance, p, sr.nearParam);
                    if (sg.nearEdge != null)
                        sg.nearEdge.edgeSegment.distanceToPseudoDistance(ref sg.minDistance, p, sg.nearParam);
                    if (sb.nearEdge != null)
                        sb.nearEdge.edgeSegment.distanceToPseudoDistance(ref sb.minDistance, p, sb.nearParam);

                    MultiDistance msd;
                    msd.r = msd.g = msd.b = msd.med = SignedDistance.INFINITE.distance;
                    if (posDist >= 0 && Math.Abs(posDist) <= Math.Abs(negDist))
                    {
                        msd.med = SignedDistance.INFINITE.distance;
                        winding = 1;
                        for (int i = 0; i < contourCount; ++i)
                            if (windings[i] > 0 && contourSD[i].med > msd.med && Math.Abs(contourSD[i].med) < Math.Abs(negDist))
                                msd = contourSD[i];
                    }
                    else if (negDist <= 0 && Math.Abs(negDist) <= Math.Abs(posDist))
                    {
                        msd.med = -SignedDistance.INFINITE.distance;
                        winding = -1;
                        for (int i = 0; i < contourCount; ++i)
                            if (windings[i] < 0 && contourSD[i].med < msd.med && Math.Abs(contourSD[i].med) < Math.Abs(posDist))
                                msd = contourSD[i];
                    }
                    for (int i = 0; i < contourCount; ++i)
                        if (windings[i] != winding && Math.Abs(contourSD[i].med) < Math.Abs(msd.med))
                            msd = contourSD[i];
                    if (median(sr.minDistance.distance, sg.minDistance.distance, sb.minDistance.distance) == msd.med)
                    {
                        msd.r = sr.minDistance.distance;
                        msd.g = sg.minDistance.distance;
                        msd.b = sb.minDistance.distance;
                    }

                    output.SetPixel(x, row,
                            new FloatRGB(
                                (float)(msd.r / range + .5),
                                (float)(msd.g / range + .5),
                                (float)(msd.b / range + .5)
                            ));
                }
            }

            if (edgeThreshold > 0)
            {
                msdfErrorCorrection(output, edgeThreshold / (scale * range));
            }

        }
        public static void generateMSDF_legacy(FloatRGBBmp output, Shape shape, double range, Vector2 scale, Vector2 translate,
            double edgeThreshold)
        {
            int w = output.Width;
            int h = output.Height;
            //#ifdef MSDFGEN_USE_OPENMP
            //    #pragma omp parallel for
            //#endif
            for (int y = 0; y < h; ++y)
            {
                int row = shape.InverseYAxis ? h - y - 1 : y;
                for (int x = 0; x < w; ++x)
                {
                    Vector2 p = (new Vector2(x + .5, y + .5) / scale) - translate;
                    EdgePoint r = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        g = new EdgePoint { minDistance = SignedDistance.INFINITE },
                        b = new EdgePoint { minDistance = SignedDistance.INFINITE };
                    //r.nearEdge = g.nearEdge = b.nearEdge = null;
                    //r.nearParam = g.nearParam = b.nearParam = 0;
                    List<Contour> contours = shape.contours;
                    int m = contours.Count;
                    for (int n = 0; n < m; ++n)
                    {
                        Contour contour = contours[n];
                        List<EdgeHolder> edges = contour.edges;
                        int j = edges.Count;
                        for (int i = 0; i < j; ++i)
                        {
                            EdgeHolder edge = edges[i];
                            double param;
                            SignedDistance distance = edge.edgeSegment.signedDistance(p, out param);

                            if (edge.HasComponent(EdgeColor.RED) && distance < r.minDistance)
                            {
                                r.minDistance = distance;
                                r.nearEdge = edge;
                                r.nearParam = param;
                            }
                            if (edge.HasComponent(EdgeColor.GREEN) && distance < g.minDistance)
                            {
                                g.minDistance = distance;
                                g.nearEdge = edge;
                                g.nearParam = param;
                            }
                            if (edge.HasComponent(EdgeColor.BLUE) && distance < b.minDistance)
                            {
                                b.minDistance = distance;
                                b.nearEdge = edge;
                                b.nearParam = param;
                            }
                        }
                        if (r.nearEdge != null)
                        {
                            r.nearEdge.edgeSegment.distanceToPseudoDistance(ref r.minDistance, p, r.nearParam);
                        }
                        if (g.nearEdge != null)
                        {
                            g.nearEdge.edgeSegment.distanceToPseudoDistance(ref g.minDistance, p, g.nearParam);
                        }
                        if (b.nearEdge != null)
                        {
                            b.nearEdge.edgeSegment.distanceToPseudoDistance(ref b.minDistance, p, b.nearParam);
                        }

                        output.SetPixel(x, row,
                            new FloatRGB(
                                (float)(r.minDistance.distance / range + .5),
                                (float)(g.minDistance.distance / range + .5),
                                (float)(b.minDistance.distance / range + .5)
                            ));
                    }
                }
            }

            if (edgeThreshold > 0)
            {
                msdfErrorCorrection(output, edgeThreshold / (scale * range));
            }
        }
    }
    public class GlyphPathBuilder : GlyphPathBuilderBase
    {
        public GlyphPathBuilder(Typeface typeface) : base(typeface) { }
    }
    public abstract class GlyphPathBuilderBase
    {
        readonly Typeface _typeface;
        TrueTypeInterpreter _trueTypeInterpreter;
        protected GlyphPointF[] _outputGlyphPoints;
        protected ushort[] _outputContours;

        //protected OpenFont.CFF.Cff1Font _ownerCff;
        //protected OpenFont.CFF.Cff1GlyphData _cffGlyphData;

        /// <summary>
        /// scale for converting latest glyph points to latest request font size
        /// </summary>
        float _recentPixelScale;

        Typography.OpenFont.CFF.CffEvaluationEngine _cffEvalEngine;

        public GlyphPathBuilderBase(Typeface typeface)
        {
            _typeface = typeface;
            this.UseTrueTypeInstructions = true;//default?
            _recentPixelScale = 1;

            //if (typeface.IsCffFont)
            {
                //	_cffEvalEngine = new OpenFont.CFF.CffEvaluationEngine();
            }
        }
        public Typeface Typeface => _typeface;
        /// <summary>
        /// use Maxim's Agg Vertical Hinting
        /// </summary>
        public bool UseVerticalHinting { get; set; }
        /// <summary>
        /// process glyph with true type instructions
        /// </summary>
        public bool UseTrueTypeInstructions { get; set; }

        /// <summary>
        /// build glyph shape from glyphIndex to be read
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <param name="sizeInPoints"></param>
        public void BuildFromGlyphIndex(ushort glyphIndex, float sizeInPoints)
        {
            BuildFromGlyph(_typeface.GetGlyphByIndex(glyphIndex), sizeInPoints);
        }
        /// <summary>
        /// build glyph shape from glyph to be read
        /// </summary>
        /// <param name="glyphIndex"></param>
        /// <param name="sizeInPoints"></param>
        public void BuildFromGlyph(Glyph glyph, float sizeInPoints)
        {
            //for true type font
            _outputGlyphPoints = glyph.GlyphPoints;
            _outputContours = glyph.EndPoints;


            //------------
            //temp fix for Cff Font
            if (glyph.IsCffGlyph)
            {
                throw new NotSupportedException();
                //_cffGlyphData = glyph.GetCff1GlyphData();
                //_ownerCff = glyph.GetOwnerCff();
            }

            //---------------



            if ((RecentFontSizeInPixels = Typeface.ConvPointsToPixels(sizeInPoints)) < 0)
            {
                //convert to pixel size
                //if size< 0 then set _recentPixelScale = 1;
                //mean that no scaling at all, we use original point value
                _recentPixelScale = 1;
            }
            else
            {
                _recentPixelScale = Typeface.CalculateScaleToPixel(RecentFontSizeInPixels);
                IsSizeChanged = true;
            }
            //-------------------------------------
            FitCurrentGlyph(glyph);
        }
        protected bool IsSizeChanged { get; set; }
        protected float RecentFontSizeInPixels { get; private set; }
        protected virtual void FitCurrentGlyph(Glyph glyph)
        {
            try
            {
                if (RecentFontSizeInPixels > 0 && UseTrueTypeInstructions &&
                    _typeface.HasPrepProgramBuffer &&
                    glyph.HasGlyphInstructions)
                {
                    if (_trueTypeInterpreter == null)
                    {
                        _trueTypeInterpreter = new TrueTypeInterpreter();
                        _trueTypeInterpreter.SetTypeFace(_typeface);
                    }
                    _trueTypeInterpreter.UseVerticalHinting = this.UseVerticalHinting;
                    //output as points,
                    _outputGlyphPoints = _trueTypeInterpreter.HintGlyph(glyph.GlyphIndex, RecentFontSizeInPixels);
                    //***
                    //all points are scaled from _trueTypeInterpreter, 
                    //so not need further scale.=> set _recentPixelScale=1
                    _recentPixelScale = 1;
                }
            }
            catch (System.Exception ex)
            {

            }
        }

        public virtual void ReadShapes(IGlyphTranslator tx)
        {
            //read output from glyph points
            //if (_cffGlyphData != null)
            {
                //	_cffEvalEngine.Run(tx, _ownerCff, _cffGlyphData, _recentPixelScale);
            }
            //else
            {
                tx.Read(_outputGlyphPoints, _outputContours, _recentPixelScale);
            }
        }
    }

    public static class GlyphPathBuilderExtensions
    {
        public static void Build(this GlyphPathBuilderBase builder, char c, float sizeInPoints)
        {
            builder.BuildFromGlyphIndex((ushort)builder.Typeface.LookupIndex(c), sizeInPoints);
        }
        public static void SetHintTechnique(this GlyphPathBuilderBase builder, HintTechnique hintTech)
        {

            builder.UseTrueTypeInstructions = false;//reset
            builder.UseVerticalHinting = false;//reset
            switch (hintTech)
            {
                case HintTechnique.TrueTypeInstruction:
                    builder.UseTrueTypeInstructions = true;
                    break;
                case HintTechnique.TrueTypeInstruction_VerticalOnly:
                    builder.UseTrueTypeInstructions = true;
                    builder.UseVerticalHinting = true;
                    break;
                case HintTechnique.CustomAutoFit:
                    //custom agg autofit 
                    builder.UseVerticalHinting = true;
                    break;
            }
        }
    }
    public enum HintTechnique : byte
    {
        /// <summary>
        /// no hinting
        /// </summary>
        None,
        /// <summary>
        /// truetype instruction
        /// </summary>
        TrueTypeInstruction,
        /// <summary>
        /// truetype instruction vertical only
        /// </summary>
        TrueTypeInstruction_VerticalOnly,
        /// <summary>
        /// custom hint
        /// </summary>
        CustomAutoFit
    }
    /// <summary>
    /// parameter for msdf generation
    /// </summary>
    public class MsdfGenParams
    {
        public float scaleX = 1;
        public float scaleY = 1;
        public float shapeScale = 1;
        public int minImgWidth = 5;
        public int minImgHeight = 5;

        public double angleThreshold = 3; //default
        public double pxRange = 180; //default
        public double edgeThreshold = 1.0f;//default,(from original code)


        public MsdfGenParams()
        {

        }
        public void SetScale(float scaleX, float scaleY)
        {
            this.scaleX = scaleX;
            this.scaleY = scaleY;
        }


    }
    public static class MsdfGlyphGen
    {
        public static Shape CreateMsdfShape(ContourBuilder glyphToContour, float pxScale)
        {
            List<Contour> cnts = glyphToContour.GetContours();
            List<Contour> newFitContours = new List<Contour>();
            int j = cnts.Count;
            for (int i = 0; i < j; ++i)
            {
                newFitContours.Add(
                    CreateFitContour(
                        cnts[i], pxScale, false, true));
            }
            return CreateMsdfShape(newFitContours);
        }
        static Shape CreateMsdfShape(List<Contour> contours)
        {
            var shape = new Shape();
            int j = contours.Count;
            for (int i = 0; i < j; ++i)
            {
                var cnt = new Msdfgen.Contour();
                shape.contours.Add(cnt);

                Contour contour = contours[i];
                List<EdgeHolder> parts = contour.edges;
                int m = parts.Count;
                for (int n = 0; n < m; ++n)
                {
                    EdgeSegment p = parts[n].edgeSegment;
                    switch (p)
                    {
                        default: throw new NotSupportedException();
                        case QuadraticSegment curve3:
                            {

                                cnt.AddQuadraticSegment(
                                    curve3.p[0].x, curve3.p[0].y,
                                    curve3.p[1].x, curve3.p[1].y,
                                    curve3.p[2].x, curve3.p[2].y
                                   );
                            }
                            break;
                        case CubicSegment curve4:
                            {
                                cnt.AddCubicSegment(
                                    curve4.p[0].x, curve4.p[0].y,
                                    curve4.p[1].x, curve4.p[1].y,
                                    curve4.p[2].x, curve4.p[2].y,
                                    curve4.p[3].x, curve4.p[3].y);
                            }
                            break;
                        case LinearSegment line:
                            {
                                cnt.AddLine(
                                    line.p[0].x, line.p[0].y,
                                    line.p[1].x, line.p[1].y);
                            }
                            break;
                    }
                }
            }
            return shape;
        }
        static Contour CreateFitContour(Contour contour, float pixelScale, bool x_axis, bool y_axis)
        {
            Contour newc = new Contour();
            List<EdgeHolder> parts = contour.edges;
            int m = parts.Count;
            for (int n = 0; n < m; ++n)
            {
                EdgeSegment p = parts[n].edgeSegment;
                switch (p)
                {
                    default: throw new NotSupportedException();
                    case QuadraticSegment curve3:
                        {
                            newc.AddQuadraticSegment(
                                    curve3.p[0].x * pixelScale, curve3.p[0].y * pixelScale,
                                    curve3.p[1].x * pixelScale, curve3.p[1].y * pixelScale,
                                    curve3.p[2].x * pixelScale, curve3.p[2].y * pixelScale);

                        }
                        break;
                    case CubicSegment curve4:
                        {
                            newc.AddCubicSegment(
                                  curve4.p[0].x * pixelScale, curve4.p[0].y * pixelScale,
                                  curve4.p[1].x * pixelScale, curve4.p[1].y * pixelScale,
                                  curve4.p[2].x * pixelScale, curve4.p[2].y * pixelScale,
                                  curve4.p[3].x * pixelScale, curve4.p[3].y * pixelScale
                                );
                        }
                        break;
                    case LinearSegment line:
                        {
                            newc.AddLine(
                                line.p[0].x * pixelScale, line.p[0].y * pixelScale,
                                line.p[1].x * pixelScale, line.p[1].y * pixelScale
                                );
                        }
                        break;
                }
            }
            return newc;
        }
        //---------------------------------------------------------------------

        public static GlyphImage CreateMsdfImage(
             ContourBuilder glyphToContour, MsdfGenParams genParams)
        {
            // create msdf shape , then convert to actual image
            return CreateMsdfImage(CreateMsdfShape(glyphToContour, genParams.shapeScale), genParams);
        }

        const double MAX = 1e240;
        public static GlyphImage CreateMsdfImage(Shape shape, MsdfGenParams genParams)
        {
            double left = MAX;
            double bottom = MAX;
            double right = -MAX;
            double top = -MAX;

            shape.findBounds(ref left, ref bottom, ref right, ref top);
            int w = (int)Math.Ceiling((right - left));
            int h = (int)Math.Ceiling((top - bottom));

            if (w < genParams.minImgWidth)
            {
                w = genParams.minImgWidth;
            }
            if (h < genParams.minImgHeight)
            {
                h = genParams.minImgHeight;
            }


            //temp, for debug with glyph 'I', tahoma font
            //double edgeThreshold = 1.00000001;//default, if edgeThreshold < 0 then  set  edgeThreshold=1 
            //Msdfgen.Vector2 scale = new Msdfgen.Vector2(0.98714652956298199, 0.98714652956298199);
            //double pxRange = 4;
            //translate = new Msdfgen.Vector2(12.552083333333332, 4.0520833333333330);
            //double range = pxRange / Math.Min(scale.x, scale.y);


            int borderW = (int)((float)w / 5f);

            //org
            //var translate = new Msdfgen.Vector2(left < 0 ? -left + borderW : borderW, bottom < 0 ? -bottom + borderW : borderW);
            //test
            var translate = new Vector2(-left + borderW, -bottom + borderW);

            w += borderW * 2; //borders,left- right
            h += borderW * 2; //borders, top- bottom

            double edgeThreshold = genParams.edgeThreshold;
            if (edgeThreshold < 0)
            {
                edgeThreshold = 1.00000001; //use default if  edgeThreshold <0
            }

            var scale = new Msdfgen.Vector2(genParams.scaleX, genParams.scaleY); //scale               
            double range = genParams.pxRange / Math.Min(scale.x, scale.y);
            //---------
            FloatRGBBmp frgbBmp = new Msdfgen.FloatRGBBmp(w, h);
            EdgeColoring.edgeColoringSimple(shape, genParams.angleThreshold);
            MsdfGenerator.generateMSDF(frgbBmp,
                shape,
                range,
                scale,
                translate,//translate to positive quadrant
                edgeThreshold);
            //-----------------------------------
            int[] buffer = MsdfGenerator.ConvertToIntBmp(frgbBmp);

            GlyphImage img = new GlyphImage(w, h);
            img.TextureOffsetX = (short)translate.x; //TODO: review here, rounding err
            img.TextureOffsetY = (short)translate.y; //TODO: review here, rounding err
            img.SetImageBuffer(buffer, false);
            return img;
        }

    }
    public class GlyphImage
    {
        int[] _pixelBuffer;
        public GlyphImage(int w, int h)
        {
            this.Width = w;
            this.Height = h;
        }
        //public RectangleF OriginalGlyphBounds { get; set; }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public bool IsBigEndian { get; private set; }

        public int BorderXY { get; set; }

        public int[] GetImageBuffer() => _pixelBuffer;
        //
        public void SetImageBuffer(int[] pixelBuffer, bool isBigEndian)
        {
            _pixelBuffer = pixelBuffer;
            this.IsBigEndian = isBigEndian;
        }
        /// <summary>
        /// texture offset X from original glyph
        /// </summary>
        public short TextureOffsetX { get; set; }
        /// <summary>
        /// texture offset Y from original glyph 
        /// </summary>
        public short TextureOffsetY { get; set; }
    }
}
