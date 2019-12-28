﻿//MIT, 2017-present, WinterDev
using System;
using PixelFarm.VectorMath;

namespace PixelFarm.Contours
{

    /// <summary>
    /// link between  (GlyphBoneJoint and Joint) or (GlyphBoneJoint and tipEdge)
    /// </summary>
    public class Bone
    {
        public readonly Joint JointA;
        public readonly Joint JointB;
        public readonly EdgeLine TipEdge;
        double _len;
#if DEBUG 
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
#endif
        public Bone(Joint a, Joint b)
        {
#if DEBUG 
            if (a == b)
            {
                throw new NotSupportedException();
            }
#endif

            JointA = a;
            JointB = b;
            Vector2f bpos = b.OriginalJointPos;
            _len = Math.Sqrt(a.CalculateSqrDistance(bpos));
            EvaluateSlope();

        }
        public Bone(Joint a, EdgeLine tipEdge)
        {

            JointA = a;
            TipEdge = tipEdge;
            Vector2f midPoint = tipEdge.GetMidPoint();
            _len = Math.Sqrt(a.CalculateSqrDistance(midPoint));
            EvaluateSlope();

        }
        public Vector2f GetVector()
        {
            if (this.JointB != null)
            {
                return JointB.OriginalJointPos - JointA.OriginalJointPos;
            }
            else if (this.TipEdge != null)
            {
                return TipEdge.GetMidPoint() - JointA.OriginalJointPos;
            }
            else
            {
                throw new NotSupportedException();
            }
        }
        public bool IsTipBone => this.TipEdge != null;

        internal void EvaluateSlope()
        {
            if (this.JointB != null)
            {
                EvaluateSlope(this.JointA.DynamicFitPos, this.JointB.DynamicFitPos);
            }
            else
            {
                //TODO: review fit pos of tip edge
                EvaluateSlope(this.JointA.DynamicFitPos, this.TipEdge.GetMidPoint());
            }
        }
        internal float EvaluateFitLength()
        {
            if (this.JointB != null)
            {
                return (float)(JointA.DynamicFitPos - JointB.DynamicFitPos).Length();
            }
            else
            {
                return (float)(JointA.DynamicFitPos - this.TipEdge.GetMidPoint()).Length();
            }
        }
        internal float EvaluateY()
        {
            if (this.JointB != null)
            {
                return (JointA.DynamicFitPos.Y + JointB.DynamicFitPos.Y) / 2;
            }
            else
            {
                return (JointA.DynamicFitPos.Y + TipEdge.GetMidPoint().Y) / 2;
            }
        }
        void EvaluateSlope(Vector2f p, Vector2f q)
        {

            double x0 = p.X;
            double y0 = p.Y;
            //q
            double x1 = q.X;
            double y1 = q.Y;

            double slopeNoDirection = Math.Abs(Math.Atan2(Math.Abs(y1 - y0), Math.Abs(x1 - x0)));
            if (x1 == x0)
            {
                this.SlopeKind = LineSlopeKind.Vertical;
            }
            else
            {
                if (slopeNoDirection > MyMath._85degreeToRad)
                {
                    SlopeKind = LineSlopeKind.Vertical;
                }
                else if (slopeNoDirection < MyMath._03degreeToRad) //_15degreeToRad
                {
                    SlopeKind = LineSlopeKind.Horizontal;
                }
                else
                {
                    SlopeKind = LineSlopeKind.Other;
                }
            }
        }

        public LineSlopeKind SlopeKind { get; private set; }
        internal double Length => _len;
        public bool IsLongBone { get; internal set; }

#if DEBUG
        public override string ToString()
        {
            if (TipEdge != null)
            {
                return dbugId + ":" + JointA.ToString() + "->" + TipEdge.GetMidPoint().ToString();
            }
            else
            {
                return dbugId + ":" + JointA.ToString() + "->" + JointB.ToString();
            }
        }
#endif
    }


    public static class GlyphBoneExtensions
    {

        //utils for glyph bones
        public static Vector2f GetMidPoint(this Bone bone)
        {
            if (bone.JointB != null)
            {
                return (bone.JointA.OriginalJointPos + bone.JointB.OriginalJointPos) / 2;
            }
            else if (bone.TipEdge != null)
            {
                Vector2f edge = bone.TipEdge.GetMidPoint();
                return (edge + bone.JointA.OriginalJointPos) / 2;
            }
            else
            {
                return Vector2f.Zero;
            }
        }


        /// <summary>
        /// find all outside edge a
        /// </summary>
        /// <param name="bone"></param>
        /// <param name="outsideEdges"></param>
        /// <returns></returns>
        public static void CollectOutsideEdge(this Bone bone, System.Collections.Generic.List<EdgeLine> outsideEdges)
        {
            if (bone.JointB != null)
            {
                AnalyzedTriangle commonTri = FindCommonTriangle(bone.JointA, bone.JointB);
                if (commonTri != null)
                {
                    if (commonTri.e0.IsOutside) { outsideEdges.Add(commonTri.e0); }
                    if (commonTri.e1.IsOutside) { outsideEdges.Add(commonTri.e1); }
                    if (commonTri.e2.IsOutside) { outsideEdges.Add(commonTri.e2); }
                }
            }
            else if (bone.TipEdge != null)
            {
                outsideEdges.Add(bone.TipEdge);
                EdgeLine found;
                if (ContainsEdge(bone.JointA.P_Tri, bone.TipEdge) &&
                    (found = FindAnotherOutsideEdge(bone.JointA.P_Tri, bone.TipEdge)) != null)
                {
                    outsideEdges.Add(found);
                }
                else if (ContainsEdge(bone.JointA.Q_Tri, bone.TipEdge) &&
                    (found = FindAnotherOutsideEdge(bone.JointA.Q_Tri, bone.TipEdge)) != null)
                {
                    outsideEdges.Add(found);
                }
            }
        }
        static EdgeLine FindAnotherOutsideEdge(AnalyzedTriangle tri, EdgeLine knownOutsideEdge)
        {
            if (tri.e0.IsOutside && tri.e0 != knownOutsideEdge) { return tri.e0; }
            if (tri.e1.IsOutside && tri.e1 != knownOutsideEdge) { return tri.e1; }
            if (tri.e2.IsOutside && tri.e2 != knownOutsideEdge) { return tri.e2; }
            return null;
        }

        static bool ContainsEdge(AnalyzedTriangle tri, EdgeLine edge)
        {
            return tri.e0 == edge || tri.e1 == edge || tri.e2 == edge;
        }
        static AnalyzedTriangle FindCommonTriangle(Joint a, Joint b)
        {

            if (a.P_Tri == b.P_Tri || a.P_Tri == b.Q_Tri)
            {
                return a.P_Tri;
            }
            else if (a.Q_Tri == b.P_Tri || a.Q_Tri == b.Q_Tri)
            {
                return a.Q_Tri;
            }
            else
            {
                return null;
            }
        }
    }

}