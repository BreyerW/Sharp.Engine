﻿/* Poly2Tri
 * Copyright (c) 2009-2010, Poly2Tri Contributors
 * http://code.google.com/p/poly2tri/
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 * * Neither the name of Poly2Tri nor the names of its contributors may be
 *   used to endorse or promote products derived from this software without specific
 *   prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Collections.Generic;
namespace Poly2Tri
{
    public sealed class TriangulationPoint
    {
        // List of edges this point constitutes an upper ending point (CDT) 
        public readonly double X;
        public readonly double Y;
        internal byte tempName; 
        public object userData;
        

        //TODO: review myEdges...
        List<DTSweepConstraint> _myEdges = new List<DTSweepConstraint>(3);
#if DEBUG
        static int dbugTotalId;
        public readonly int dbugId = dbugTotalId++;
#endif
        public TriangulationPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
        public TriangulationPoint(double x, double y, object userData)
        {
            X = x;
            Y = y;
            this.userData = userData;
        }
        public override string ToString() => "[" + X + "," + Y + "]";

        public float Xf => (float)X;

        public float Yf => (float)Y;

        public void AddEdge(DTSweepConstraint e)
        {
            //TODO: review myEdges, ***
            _myEdges.Add(e);
        }

        internal List<DTSweepConstraint> GetInternalEdgeList() => _myEdges;

#if DEBUG
        public static bool dbugIsEqualPointCoord(TriangulationPoint a, TriangulationPoint b)
        {
            return a.X == b.X && a.Y == b.Y;
        }
#endif
    }
}