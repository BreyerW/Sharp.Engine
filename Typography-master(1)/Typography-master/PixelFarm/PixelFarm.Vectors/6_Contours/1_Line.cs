﻿//MIT, 2017-present, WinterDev
using System;
using PixelFarm.VectorMath;

namespace PixelFarm.Contours
{
    public enum LineSlopeKind : byte
    {
        Vertical,
        Horizontal,
        Other
    }


    /// <summary>
    /// edge of GlyphTriangle
    /// </summary>
    public abstract class EdgeLine
    {
        internal readonly Vertex _glyphPoint_P;
        internal readonly Vertex _glyphPoint_Q;
        AnalyzedTriangle _ownerTriangle;

        internal EdgeLine(AnalyzedTriangle ownerTriangle, Vertex p, Vertex q)
        {
            //this canbe inside edge or outside edge

            _ownerTriangle = ownerTriangle;
            //------------------------------------
            //an edge line connects 2 glyph points.
            //it is created from triangulation process.
            //
            //some edge line is either 'INSIDE' edge  OR 'OUTSIDE'.
            //
            //------------------------------------   
            _glyphPoint_P = p;
            _glyphPoint_Q = q;

            //new dynamic mid point is calculate from original X,Y 
            //-------------------------------
            //analyze angle and slope kind
            //-------------------------------  

            //slope kind is evaluated

            SlopeAngleNoDirection = this.GetSlopeAngleNoDirection();
            if (QX == PX)
            {
                this.SlopeKind = LineSlopeKind.Vertical;
            }
            else
            {

                if (SlopeAngleNoDirection > _85degreeToRad)
                {
                    SlopeKind = LineSlopeKind.Vertical;
                }
                else if (SlopeAngleNoDirection < _01degreeToRad)
                {
                    SlopeKind = LineSlopeKind.Horizontal;
                    p.IsPartOfHorizontalEdge = q.IsPartOfHorizontalEdge = true;
                }
                else
                {
                    SlopeKind = LineSlopeKind.Other;
                }
            }
        }

        /// <summary>
        /// original px
        /// </summary>
        public double PX => _glyphPoint_P.OX;
        /// <summary>
        /// original py
        /// </summary>
        public double PY => _glyphPoint_P.OY;
        /// <summary>
        /// original qx
        /// </summary>
        public double QX => _glyphPoint_Q.OX;
        /// <summary>
        /// original qy
        /// </summary>
        public double QY => _glyphPoint_Q.OY;


        public bool IsTip { get; internal set; }

        internal Vector2f GetOriginalEdgeVector()
        {
            return new Vector2f(
                Q.OX - _glyphPoint_P.OX,
                Q.OY - _glyphPoint_P.OY);
        }


        public Vertex P => _glyphPoint_P;
        public Vertex Q => _glyphPoint_Q;
        public LineSlopeKind SlopeKind { get; internal set; }

        internal AnalyzedTriangle OwnerTriangle => _ownerTriangle;

        public abstract bool IsOutside { get; }
        public bool IsInside => !this.IsOutside;
        public bool IsUpper { get; internal set; }
        public bool IsLeftSide { get; internal set; }
        internal double SlopeAngleNoDirection { get; private set; }
        public override string ToString()
        {
            return SlopeKind + ":" + PX + "," + PY + "," + QX + "," + QY;
        }

        static readonly double _85degreeToRad = MyMath.DegreesToRadians(85);
        static readonly double _01degreeToRad = MyMath.DegreesToRadians(1);

        internal bool _earlyInsideAnalysis;
        internal bool ContainsGlyphPoint(Vertex p)
        {
            return _glyphPoint_P == p || _glyphPoint_Q == p;
        }

        /// <summary>
        /// find common edge of 2 glyph points
        /// </summary>
        /// <param name="p"></param>
        /// <param name="q"></param>
        /// <returns></returns>
        internal static OutsideEdgeLine FindCommonOutsideEdge(Vertex p, Vertex q)
        {
            if (p.E0 == q.E0 ||
                p.E0 == q.E1)
            {
                return p.E0;
            }
            else if (p.E1 == q.E0 ||
                     p.E1 == q.E1)
            {
                return p.E1;
            }
            else
            {

                return null;
            }
        }
#if DEBUG
        public bool dbugNoPerpendicularBone { get; set; }
        public static int s_dbugTotalId;
        public readonly int dbugId = s_dbugTotalId++;
#endif

    }

    public class OutsideEdgeLine : EdgeLine
    {
        internal Vector2f _newDynamicMidPoint;
        //if this edge is 'OUTSIDE',
        //it have 1-2 control(s) edge (inside)
        EdgeLine _ctrlEdge_P;
        EdgeLine _ctrlEdge_Q;
        internal OutsideEdgeLine(AnalyzedTriangle ownerTriangle, Vertex p, Vertex q)
            : base(ownerTriangle, p, q)
        {

            //set back
            p.SetOutsideEdgeUnconfirmEdgeDirection(this);
            q.SetOutsideEdgeUnconfirmEdgeDirection(this);
            _newDynamicMidPoint = new Vector2f((p.OX + q.OX) / 2, (p.OY + q.OY) / 2);
        }
        internal void SetDynamicEdgeOffsetFromMasterOutline(float newEdgeOffsetFromMasterOutline)
        {

            //TODO: refactor here...
            //this is relative len from current edge              
            //origianl vector
            Vector2f _o_edgeVector = GetOriginalEdgeVector();
            //rotate 90
            Vector2f _rotate = _o_edgeVector.Rotate(90);
            //
            Vector2f _deltaVector = _rotate.NewLength(newEdgeOffsetFromMasterOutline);

            //new dynamic mid point  
            _newDynamicMidPoint = this.GetMidPoint() + _deltaVector;
        }
        //
        public override bool IsOutside => true;
        public EdgeLine ControlEdge_P => _ctrlEdge_P;
        public EdgeLine ControlEdge_Q => _ctrlEdge_Q;
        //
        internal void SetControlEdge(EdgeLine controlEdge)
        {
            //check if edge is connect to p or q

#if DEBUG
            if (!controlEdge.IsInside)
            {

            }
#endif
            //----------------
            if (_glyphPoint_P == controlEdge._glyphPoint_P)
            {
#if DEBUG
                if (_ctrlEdge_P != null && _ctrlEdge_P != controlEdge)
                {
                }
#endif
                //map this p to p of the control edge
                _ctrlEdge_P = controlEdge;

            }
            else if (_glyphPoint_P == controlEdge.Q)
            {
#if DEBUG
                if (_ctrlEdge_P != null && _ctrlEdge_P != controlEdge)
                {
                }
#endif
                _ctrlEdge_P = controlEdge;
            }
            else if (_glyphPoint_Q == controlEdge._glyphPoint_P)
            {
#if DEBUG
                if (_ctrlEdge_Q != null && _ctrlEdge_Q != controlEdge)
                {
                }
#endif
                _ctrlEdge_Q = controlEdge;
            }
            else if (_glyphPoint_Q == controlEdge.Q)
            {
#if DEBUG
                if (_ctrlEdge_Q != null && _ctrlEdge_Q != controlEdge)
                {
                }
#endif
                _ctrlEdge_Q = controlEdge;
            }
            else
            {
                //?
            }
        }
    }
    public class InsideEdgeLine : EdgeLine
    {

        internal Joint _inside_joint;
        internal InsideEdgeLine(AnalyzedTriangle ownerTriangle, Vertex p, Vertex q)
            : base(ownerTriangle, p, q)
        {
        }
        public override bool IsOutside => false;
    }
    public static class EdgeLineExtensions
    {
        public static Vector2f GetMidPoint(this EdgeLine line)
        {
            return new Vector2f((float)((line.PX + line.QX) / 2), (float)((line.PY + line.QY) / 2));
        }

        internal static double GetSlopeAngleNoDirection(this EdgeLine line)
        {
            return Math.Abs(Math.Atan2(Math.Abs(line.QY - line.PY), Math.Abs(line.QX - line.PX)));
        }

        internal static bool ContainsTriangle(this EdgeLine edge, AnalyzedTriangle p)
        {
            return (p.e0 == edge ||
                    p.e1 == edge ||
                    p.e2 == edge);
        }
#if DEBUG
        public static void dbugGetScaledXY(this EdgeLine edge, out double px, out double py, out double qx, out double qy, float scale)
        {
            px = edge.PX * scale;
            py = edge.PY * scale;
            //
            qx = edge.QX * scale;
            qy = edge.QY * scale;

        }
#endif
    }
}