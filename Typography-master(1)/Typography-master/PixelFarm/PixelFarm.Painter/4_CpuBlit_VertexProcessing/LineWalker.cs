﻿//MIT, 2014-present, WinterDev
//----------------------------------------------------------------------------
//some part from
// Anti-Grain Geometry - Version 2.4 
// Copyright (C) 2002-2005 Maxim Shemanarev (http://www.antigrain.com)
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

using System;
using System.Collections.Generic;
using PixelFarm.Drawing;
namespace PixelFarm.CpuBlit.VertexProcessing
{

    public delegate void LineSegmentDelegate(VertexStore vxs, VertexCmd cmd, double x, double y);

    public class LineWalkerMark
    {
        readonly internal LineSegmentDelegate lineSegDel;
        public LineWalkerMark(double len, LineSegmentDelegate lineSegDel)
        {
            this.Len = len;
            this.lineSegDel = lineSegDel;
        }
        public double Len { get; set; }
    }

    public enum LineWalkDashStyle
    {
        Blank,
        Solid,
    }
    public static class LineWalkerExtensions
    {
        public static void AddMark(this LineWalker walker, double len, LineSegmentDelegate segDel)
        {
            var mark = new LineWalkerMark(len, segDel);
            walker.AddWalkMark(mark);
        }
        public static void AddMark(this LineWalker walker, double len, LineWalkDashStyle daskStyle)
        {
            switch (daskStyle)
            {
                default: throw new NotSupportedException();
                case LineWalkDashStyle.Solid:
                    walker.AddWalkMark(new LineWalkerMark(len, SimpleSolidLine));
                    break;
                case LineWalkDashStyle.Blank:
                    walker.AddWalkMark(new LineWalkerMark(len, SimpleBlankLine));
                    break;
            }
        }
        static void SimpleSolidLine(VertexStore outputVxs, VertexCmd cmd, double x, double y)
        {
            //solid               
            switch (cmd)
            {
                default: throw new NotSupportedException();

                case VertexCmd.MoveTo:
                    outputVxs.AddMoveTo(x, y);
                    break;
                case VertexCmd.LineTo:
                    outputVxs.AddLineTo(x, y);
                    break;
            }
        }
        static void SimpleBlankLine(VertexStore outputVxs, VertexCmd cmd, double x, double y)
        {

        }
    }
    public class LineWalker
    {
        WalkStateManager _walkStateMan = new WalkStateManager(); 
        public LineWalker()
        {
        }
        public void ClearMarks()
        {
            _walkStateMan.ClearAllMarkers();
        }
        public void AddWalkMark(LineWalkerMark walkerMark)
        {
            _walkStateMan.AddSegmentMark(walkerMark);
        }

        public void Walk(VertexStore src, VertexStore output)
        {
            //
            //we do not flatten the curve 
            // 
            _walkStateMan._output = output;
            _walkStateMan.Reset();
            int count = src.Count;
            VertexCmd cmd;
            double x, y;
            for (int i = 0; i < count; ++i)
            {
                cmd = src.GetVertex(i, out x, out y);
                switch (cmd)
                {
                    case VertexCmd.MoveTo:
                        _walkStateMan.MoveTo(x, y);
                        break;
                    case VertexCmd.NoMore:
                        i = count + 1; //force end => EXIT_LOOP
                        break;
                    case VertexCmd.LineTo:
                        _walkStateMan.LineTo(x, y);
                        break;
                    case VertexCmd.C3:
                    case VertexCmd.C4:
                        throw new NotSupportedException();
                    case VertexCmd.Close:
                        _walkStateMan.CloseFigure();
                        break;
                }
            }

        }
        enum WalkState
        {
            Init,
            PolyLine,
        }


        class WalkStateManager
        {

            List<LineWalkerMark> _marks = new List<LineWalkerMark>();
            LineWalkerMark _currentMark;
            int _nextMarkNo;

            double _expectedSegmentLen;
            WalkState _state;
            double _latest_X, _latest_Y;
            double _latest_moveto_X, _latest_moveto_Y;

            double _total_accum_len;
            List<TmpPoint> _tempPoints = new List<TmpPoint>();


            //-------------------------------
            internal VertexStore _output;

            public void AddSegmentMark(LineWalkerMark segMark)
            {
                _marks.Add(segMark);
            }
            public void ClearAllMarkers()
            {
                _marks.Clear();
                Reset();
            }
            public void Reset()
            {

                _currentMark = null;
                _nextMarkNo = 0;
                _total_accum_len = 0;
                _expectedSegmentLen = 0;
                _state = WalkState.Init;
                _nextMarkNo = 0;
                _latest_X = _latest_Y =
                    _latest_moveto_Y = _latest_moveto_Y = 0;
            }
            //-----------------------------------------------------
            void StepToNextMarkerSegment()
            {
                _currentMark = _marks[_nextMarkNo];
                _expectedSegmentLen = _currentMark.Len;
                if (_nextMarkNo + 1 < _marks.Count)
                {
                    _nextMarkNo++;
                }
                else
                {
                    _nextMarkNo = 0;
                }
            }
            public void CloseFigure()
            {
                LineTo(_latest_moveto_X, _latest_moveto_Y);
                //close current figure
                //***                      

                ClearCollectedTmpPoints(out _expectedSegmentLen);
            }
            void ClearCollectedTmpPoints(out double tmp_expectedLen)
            {
                //clear all previous collected points
                int j = _tempPoints.Count;
                tmp_expectedLen = 0;
                if (j > 0)
                {
                    tmp_expectedLen = _expectedSegmentLen;
                    for (int i = 0; i < j;)
                    {
                        //p0-p1
                        TmpPoint p0 = _tempPoints[i];
                        TmpPoint p1 = _tempPoints[i + 1];
                        //-------------------------------
                        //a series of connected line
                        //if ((_nextMarkNo % 2) == 1)
                        //{
                        //    if (i == 0)
                        //    {
                        //        //move to
                        //        _output.AddMoveTo(p0.x, p0.y);
                        //    }
                        //    _output.AddLineTo(p1.x, p1.y);
                        //}
                        if (i == 0)
                        {
                            //1st move to
                            _currentMark.lineSegDel(_output, VertexCmd.MoveTo, p0.x, p0.y);
                        }
                        _currentMark.lineSegDel(_output, VertexCmd.LineTo, p1.x, p1.y);

                        //-------------------------------
                        double len = AggMath.calc_distance(p0.x, p0.y, p1.x, p1.y);
                        tmp_expectedLen -= len;
                        i += 2;
                        _latest_X = p1.x;
                        _latest_Y = p1.y;
                    }
                    //------------



                    _tempPoints.Clear();
                }
                //-----------------
            }
            public void MoveTo(double x0, double y0)
            {
                switch (_state)
                {
                    default: throw new NotSupportedException();
                    case WalkState.Init:
                        _latest_moveto_X = _latest_X = x0;
                        _latest_moveto_Y = _latest_Y = y0;
                        StepToNextMarkerSegment();//start read
                        OnMoveTo();
                        break;
                    case WalkState.PolyLine:
                        //stop current line 
                        {

                        }
                        break;
                }
            }
            public void LineTo(double x1, double y1)
            {
                switch (_state)
                {
                    default: throw new NotSupportedException();
                    case WalkState.Init:

                        _state = WalkState.PolyLine;
                        goto case WalkState.PolyLine;
                    case WalkState.PolyLine:
                        {

                            //clear prev segment len  
                            //find line segment length 
                            double new_remaining_len = AggMath.calc_distance(_latest_X, _latest_Y, x1, y1);
                            //check current gen state
                            //find angle
                            double angle = Math.Atan2(y1 - _latest_Y, x1 - _latest_X);
                            double sin = Math.Sin(angle);
                            double cos = Math.Cos(angle);
                            double new_x, new_y;

                            OnBeginLineSegment(sin, cos, ref new_remaining_len);

                            while (new_remaining_len >= _expectedSegmentLen)
                            {
                                //we can create a new segment
                                new_x = _latest_X + (_expectedSegmentLen * cos);
                                new_y = _latest_Y + (_expectedSegmentLen * sin);
                                new_remaining_len -= _expectedSegmentLen;
                                //each segment has its own line production procedure
                                //eg.  
                                OnSegment(new_x, new_y);
                                //--------------------
                                _latest_Y = new_y;
                                _latest_X = new_x;
                            }
                            //set on corner 
                            OnEndLineSegment(x1, y1, new_remaining_len);
                        }
                        break;
                }
            }
            protected virtual void OnBeginLineSegment(double sin, double cos, ref double new_remaining_len)
            {
                if (_total_accum_len > 0)
                {
                    //there is an incomplete len from prev step
                    //check if we can create a segment or not
                    if (_total_accum_len + new_remaining_len >= _expectedSegmentLen)
                    {
                        //***                        
                        //clear all previous collected points
                        double tmp_expectedLen;
                        ClearCollectedTmpPoints(out tmp_expectedLen);
                        //-----------------
                        //begin
                        if (tmp_expectedLen > 0)
                        {
                            //we can create a new segment
                            double new_x = _latest_X + (tmp_expectedLen * cos);
                            double new_y = _latest_Y + (tmp_expectedLen * sin);
                            new_remaining_len -= _expectedSegmentLen;
                            //each segment has its own line production procedure
                            //eg.  
                            _currentMark.lineSegDel(_output, VertexCmd.LineTo, _latest_X = new_x, _latest_Y = new_y);
                            StepToNextMarkerSegment();
                        }
                        //-----------------   
                        _total_accum_len = 0;
                    }
                    else
                    {

                    }
                }
            }


            struct TmpPoint
            {
                public readonly double x;
                public readonly double y;
                public TmpPoint(double x, double y)
                {

                    this.x = x;
                    this.y = y;
                }
#if DEBUG
                public override string ToString()
                {
                    return "(" + x + "," + y + ")";
                }
#endif
            }


            protected virtual void OnEndLineSegment(double x, double y, double remainingLen)
            {
                //remainingLen of current segment
                if (remainingLen >= 0.5)
                {
                    //TODO: review here, if remainingLen is too small,
                    //but what about _total_accum_len

                    //there are remaining segment that can be complete at this state
                    //so we just collect it                                        
                    _total_accum_len += remainingLen;
                    _tempPoints.Add(new TmpPoint(_latest_X, _latest_Y));
                    _tempPoints.Add(new TmpPoint(x, y));
                }
                _latest_X = x;
                _latest_Y = y;
            }
            protected virtual void OnMoveTo()
            {

            }


            protected virtual void OnSegment(double new_x, double new_y)
            {
                _currentMark.lineSegDel(_output, VertexCmd.MoveTo, _latest_X, _latest_Y);
                _currentMark.lineSegDel(_output, VertexCmd.LineTo, new_x, new_y);

                _total_accum_len = 0;
                StepToNextMarkerSegment();
            }
        }

    }

}