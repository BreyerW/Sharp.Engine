using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace BepuPhysics.CollisionDetection
{
    /// <summary>
    /// Defines a type that can act as a callback for broad phase sweep tests.
    /// </summary>
    public interface IBroadPhaseSweepTester
    {
        unsafe void Test(CollidableReference collidable, ref float maximumT);
    }
    public interface IBroadPhaseFrustumTester
    {
        unsafe void FrustumTest(CollidableReference collidable, FrustumData* frustumData);
    }
    partial class BroadPhase
    {
        struct RayLeafTester<TRayTester> : IRayLeafTester where TRayTester : IBroadPhaseRayTester
        {
            public TRayTester LeafTester;
            public Buffer<CollidableReference> Leaves;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void TestLeaf(int leafIndex, RayData* rayData, float* maximumT)
            {
                LeafTester.RayTest(Leaves[leafIndex], rayData, maximumT);
            }
        }
        struct FrustumLeafTester<TFrustumTester> : IFrustumLeafTester where TFrustumTester : IBroadPhaseFrustumTester
        {
            public TFrustumTester LeafTester;
            public Buffer<CollidableReference> Leaves;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void TestLeaf(int leafIndex, FrustumData* frustumData)
            {
                LeafTester.FrustumTest(Leaves[leafIndex], frustumData);
            }
        }
        /// <summary>
        /// Finds any intersections between a ray and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TRayTester">Type of the callback to execute on ray-leaf bounding box intersections.</typeparam>
        /// <param name="origin">Origin of the ray to cast.</param>
        /// <param name="direction">Direction of the ray to cast.</param>
        /// <param name="maximumT">Maximum length of the ray traversal in units of the direction's length.</param>
        /// <param name="rayTester">Callback to execute on ray-leaf bounding box intersections.</param>
        /// <param name="id">User specified id of the ray.</param>
        public unsafe void RayCast<TRayTester>(in Vector3 origin, in Vector3 direction, float maximumT, ref TRayTester rayTester, int id = 0) where TRayTester : IBroadPhaseRayTester
        {
            TreeRay.CreateFrom(origin, direction, maximumT, id, out var rayData, out var treeRay);
            RayLeafTester<TRayTester> tester;
            tester.LeafTester = rayTester;
            tester.Leaves = activeLeaves;
            ActiveTree.RayCast(&treeRay, &rayData, ref tester);
            tester.Leaves = staticLeaves;
            StaticTree.RayCast(&treeRay, &rayData, ref tester);
            //The sweep tester probably relies on mutation to function; copy any mutations back to the original reference.
            rayTester = tester.LeafTester;
        }
        /// <summary>
        /// Finds any intersections between a frustum and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TFrustumTester">Type of the callback to execute on frustum-leaf bounding box intersections.</typeparam>
        /// <param name="matrix">should be multiply of inversed view matrix (or camera's model matrix) and projection matrix </param>
        /// <param name="frustumTester">Callback to execute on frustum-leaf bounding box intersections.</param>
        /// <param name="columnMajor">matrix is column-major or not</param>
        /// <param name="id">User specified id of the frustum.</param>
        public unsafe void FrustumSweep<TFrustumTester>(in Matrix4x4 matrix, ref TFrustumTester frustumTester, bool columnMajor = false, int id = 0, IThreadDispatcher dispatcher = null) where TFrustumTester : IBroadPhaseFrustumTester
        {
            //TODO: maybe use preprocessor directive instead of bool?
            //#if COLUMNMAJOR
            var frustumData = new FrustumData()
            {
                Id = id
            };
            var planeSpan = new Span<Plane>(&frustumData.nearPlane, 18);

            if (columnMajor)
            {
                ref var refMat = ref Unsafe.AsRef(matrix);
                ref var lastColumn = ref Unsafe.As<float, Vector4>(ref refMat.M41);
                ref var firstColumn = ref Unsafe.As<float, Vector4>(ref refMat.M11);
                ref var secondColumn = ref Unsafe.As<float, Vector4>(ref refMat.M21);
                ref var thirdColumn = ref Unsafe.As<float, Vector4>(ref refMat.M31);

                // Near clipping plane
                planeSpan[0] = Plane.Normalize(new Plane(lastColumn + thirdColumn));

                // Top clipping plane
                planeSpan[2] = Plane.Normalize(new Plane(lastColumn - secondColumn));

                // Bottom clipping plane
                planeSpan[4] = Plane.Normalize(new Plane(lastColumn + secondColumn));

                // Left clipping plane
                planeSpan[6] = Plane.Normalize(new Plane(lastColumn + firstColumn));

                // Right clipping plane
                planeSpan[8] = Plane.Normalize(new Plane(lastColumn - firstColumn));

                // Far clipping plane
                planeSpan[10] = Plane.Normalize(new Plane(lastColumn - thirdColumn));

            }
            else
            {
                var lastColumn = new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);
                var firstColumn = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
                var secondColumn = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
                var thirdColumn = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);

                // Near clipping plane
                planeSpan[0] = Plane.Normalize(new Plane(matrix.M13, matrix.M23, matrix.M33, matrix.M43));

                // Top clipping plane
                planeSpan[2] = Plane.Normalize(new Plane(lastColumn - secondColumn));

                // Bottom clipping plane
                planeSpan[4] = Plane.Normalize(new Plane(lastColumn + secondColumn));

                // Left clipping plane
                planeSpan[6] = Plane.Normalize(new Plane(lastColumn + firstColumn));

                // Right clipping plane
                planeSpan[8] = Plane.Normalize(new Plane(lastColumn - firstColumn));

                // Far clipping plane
                planeSpan[10] = Plane.Normalize(new Plane(lastColumn - thirdColumn));
            }

            planeSpan[1].Normal = Vector3.Abs(planeSpan[0].Normal);

            planeSpan[3].Normal = Vector3.Abs(planeSpan[2].Normal);

            planeSpan[5].Normal = Vector3.Abs(planeSpan[4].Normal);

            planeSpan[7].Normal = Vector3.Abs(planeSpan[6].Normal);

            planeSpan[9].Normal = Vector3.Abs(planeSpan[8].Normal);

            planeSpan[11].Normal = Vector3.Abs(planeSpan[10].Normal);

            planeSpan[0].D *= -2;
            planeSpan[2].D *= -2;
            planeSpan[4].D *= -2;
            planeSpan[6].D *= -2;
            planeSpan[8].D *= -2;
            planeSpan[10].D *= -2;

            /*ref Vector<float> n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[0]);
			var mask = Vector.GreaterThan<float>(Vector<float>.Zero, n);
			planeSpan[12].Normal = new Vector3(mask[0], mask[1], mask[2]); //new Vector3(n.X > 0f ? 1f : 0f, n.Y > 0f ? 1f : 0f, n.Z > 0f ? 1f : 0f);

			n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[2]);
			mask = Vector.GreaterThan<float>(Vector<float>.Zero, n);
			planeSpan[13].Normal = new Vector3(mask[0], mask[1], mask[2]);

			n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[4]);
			mask = Vector.GreaterThan<float>(Vector<float>.Zero, n);
			planeSpan[14].Normal = new Vector3(mask[0], mask[1], mask[2]);

			n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[6]);
			mask = Vector.GreaterThan<float>(Vector<float>.Zero, n);
			planeSpan[15].Normal = new Vector3(mask[0], mask[1], mask[2]);

			n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[8]);
			mask = Vector.GreaterThan<float>(Vector<float>.Zero, n);
			planeSpan[16].Normal = new Vector3(mask[0], mask[1], mask[2]);

			n = ref Unsafe.As<Plane, Vector<float>>(ref planeSpan[10]);
			mask = Vector.GreaterThanOrEqual<float>(Vector<float>.Zero, n);
			planeSpan[17].Normal = new Vector3(mask[0], mask[1], mask[2]);
			*/

            FrustumLeafTester<TFrustumTester> tester;
            tester.LeafTester = frustumTester;
            /*tester.Leaves = activeLeaves;
			ActiveTree.FrustumSweep(&frustumData, ref tester);
			tester.Leaves = staticLeaves;
			frustumData.treeId++;
			StaticTree.FrustumSweep(&frustumData, ref tester);
			frustumData.treeId++;*/
            tester.Leaves = frozenLeaves;
            if (dispatcher is null)
                FrozenTree.FrustumSweep(&frustumData, Pool, ref tester);
            else
                FrozenTree.FrustumSweepMultithreaded(&frustumData, Pool, ref tester, dispatcher);
            //The sweep tester probably relies on mutation to function; copy any mutations back to the original reference.
            frustumTester = tester.LeafTester;
        }

        struct SweepLeafTester<TSweepTester> : ISweepLeafTester where TSweepTester : IBroadPhaseSweepTester
        {
            public TSweepTester LeafTester;
            public Buffer<CollidableReference> Leaves;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void TestLeaf(int leafIndex, ref float maximumT)
            {
                LeafTester.Test(Leaves[leafIndex], ref maximumT);
            }

        }

        /// <summary>
        /// Finds any intersections between a swept bounding box and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TSweepTester">Type of the callback to execute on sweep-leaf bounding box intersections.</typeparam>
        /// <param name="min">Minimum bounds of the box to sweep.</param>
        /// <param name="max">Maximum bounds of the box to sweep.</param>
        /// <param name="direction">Direction along which to sweep the bounding box.</param>
        /// <param name="maximumT">Maximum length of the sweep in units of the direction's length.</param>
        /// <param name="sweepTester">Callback to execute on sweep-leaf bounding box intersections.</param>
        public unsafe void Sweep<TSweepTester>(in Vector3 min, in Vector3 max, in Vector3 direction, float maximumT, ref TSweepTester sweepTester) where TSweepTester : IBroadPhaseSweepTester
        {
            Tree.ConvertBoxToCentroidWithExtent(min, max, out var origin, out var expansion);
            TreeRay.CreateFrom(origin, direction, maximumT, out var treeRay);
            SweepLeafTester<TSweepTester> tester;
            tester.LeafTester = sweepTester;
            tester.Leaves = activeLeaves;
            ActiveTree.Sweep(expansion, origin, direction, &treeRay, ref tester);
            tester.Leaves = staticLeaves;
            StaticTree.Sweep(expansion, origin, direction, &treeRay, ref tester);
            //The sweep tester probably relies on mutation to function; copy any mutations back to the original reference.
            sweepTester = tester.LeafTester;
        }

        /// <summary>
        /// Finds any intersections between a swept bounding box and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TSweepTester">Type of the callback to execute on sweep-leaf bounding box intersections.</typeparam>
        /// <param name="boundingBox">Bounding box to sweep.</param>
        /// <param name="direction">Direction along which to sweep the bounding box.</param>
        /// <param name="maximumT">Maximum length of the sweep in units of the direction's length.</param>
        /// <param name="sweepTester">Callback to execute on sweep-leaf bounding box intersections.</param>
        public unsafe void Sweep<TSweepTester>(in BoundingBox boundingBox, in Vector3 direction, float maximumT, ref TSweepTester sweepTester) where TSweepTester : IBroadPhaseSweepTester
        {
            Sweep(boundingBox.Min, boundingBox.Max, direction, maximumT, ref sweepTester);
        }

        struct BoxQueryEnumerator<TInnerEnumerator> : IBreakableForEach<int> where TInnerEnumerator : IBreakableForEach<CollidableReference>
        {
            public TInnerEnumerator Enumerator;
            public Buffer<CollidableReference> Leaves;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool LoopBody(int i)
            {
                return Enumerator.LoopBody(Leaves[i]);
            }
        }

        /// <summary>
        /// Finds any overlaps between a bounding box and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TOverlapEnumerator">Type of the enumerator to call for overlaps.</typeparam>
        /// <param name="min">Minimum bounds of the query box.</param>
        /// <param name="max">Maximum bounds of the query box.</param>
        /// <param name="overlapEnumerator">Enumerator to call for overlaps.</param>
        public unsafe void GetOverlaps<TOverlapEnumerator>(in Vector3 min, in Vector3 max, ref TOverlapEnumerator overlapEnumerator) where TOverlapEnumerator : IBreakableForEach<CollidableReference>
        {
            BoxQueryEnumerator<TOverlapEnumerator> enumerator;
            enumerator.Enumerator = overlapEnumerator;
            enumerator.Leaves = activeLeaves;
            ActiveTree.GetOverlaps(min, max, ref enumerator);
            enumerator.Leaves = staticLeaves;
            StaticTree.GetOverlaps(min, max, ref enumerator);
            //Enumeration could have mutated the enumerator; preserve those modifications.
            overlapEnumerator = enumerator.Enumerator;
        }

        /// <summary>
        /// Finds any overlaps between a bounding box and leaf bounding boxes.
        /// </summary>
        /// <typeparam name="TOverlapEnumerator">Type of the enumerator to call for overlaps.</typeparam>
        /// <param name="boundingBox">Query box bounds.</param>
        /// <param name="overlapEnumerator">Enumerator to call for overlaps.</param>
        public unsafe void GetOverlaps<TOverlapEnumerator>(in BoundingBox boundingBox, ref TOverlapEnumerator overlapEnumerator) where TOverlapEnumerator : IBreakableForEach<CollidableReference>
        {
            BoxQueryEnumerator<TOverlapEnumerator> enumerator;
            enumerator.Enumerator = overlapEnumerator;
            enumerator.Leaves = activeLeaves;
            ActiveTree.GetOverlaps(boundingBox, ref enumerator);
            enumerator.Leaves = staticLeaves;
            StaticTree.GetOverlaps(boundingBox, ref enumerator);
            //Enumeration could have mutated the enumerator; preserve those modifications.
            overlapEnumerator = enumerator.Enumerator;
        }
    }
}
