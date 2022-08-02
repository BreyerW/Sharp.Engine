﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuUtilities.Memory;
using System.Diagnostics;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuPhysics.Trees;
using BepuPhysics.CollisionDetection.CollisionTasks;

namespace BepuPhysics.Collidables
{
    /// <summary>
    /// Compound shape containing a bunch of shapes accessible through a tree acceleration structure. Useful for compounds with lots of children.
    /// </summary>
    public struct BigCompound : ICompoundShape
    {
        /// <summary>
        /// Acceleration structure for the compound children.
        /// </summary>
        public Tree Tree;
        /// <summary>
        /// Buffer of children within this compound.
        /// </summary>
        public Buffer<CompoundChild> Children;

        /// <summary>
        /// Creates a compound shape with an acceleration structure.
        /// </summary>
        /// <param name="children">Set of children in the compound.</param>
        /// <param name="shapes">Shapes set in which child shapes are allocated.</param>
        /// <param name="pool">Pool to use to allocate acceleration structures.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BigCompound(Buffer<CompoundChild> children, Shapes shapes, BufferPool pool)
        {
            Debug.Assert(children.Length > 0, "Compounds must have a nonzero number of children.");
            Debug.Assert(Compound.ValidateChildIndices(ref children, shapes), "Children must all be convex.");
            Children = children;
            Tree = new Tree(pool, children.Length);
            pool.Take(children.Length, out Buffer<BoundingBox> leafBounds);
            Compound.ComputeChildBounds(Children[0], Quaternion.Identity, shapes, out leafBounds[0].Min, out leafBounds[0].Max);
            for (int i = 1; i < Children.Length; ++i)
            {
                ref var bounds = ref leafBounds[i];
                Compound.ComputeChildBounds(Children[i], Quaternion.Identity, shapes, out bounds.Min, out bounds.Max);
            }
            Tree.SweepBuild(pool, leafBounds);
            pool.Return(ref leafBounds);
        }

        public void ComputeBounds(in Quaternion orientation, Shapes shapeBatches, out Vector3 min, out Vector3 max)
        {
            Compound.ComputeChildBounds(Children[0], orientation, shapeBatches, out min, out max);
            for (int i = 1; i < Children.Length; ++i)
            {
                ref var child = ref Children[i];
                Compound.ComputeChildBounds(Children[i], orientation, shapeBatches, out var childMin, out var childMax);
                BoundingBox.CreateMerged(min, max, childMin, childMax, out min, out max);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddChildBoundsToBatcher(ref BoundingBoxBatcher batcher, in RigidPose pose, in BodyVelocity velocity, int bodyIndex)
        {
            Compound.AddChildBoundsToBatcher(ref Children, ref batcher, pose, velocity, bodyIndex);
        }

        unsafe struct LeafTester<TRayHitHandler> : IRayLeafTester where TRayHitHandler : struct, IShapeRayHitHandler
        {
            public CompoundChild* Children;
            public Shapes Shapes;
            public TRayHitHandler Handler;
            public Matrix3x3 Orientation;
            public RayData OriginalRay;


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public LeafTester(in Buffer<CompoundChild> children, Shapes shapes, in TRayHitHandler handler, in Matrix3x3 orientation, in RayData originalRay)
            {
                Children = children.Memory;
                Shapes = shapes;
                Handler = handler;
                Orientation = orientation;
                OriginalRay = originalRay;
            }



            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe void TestLeaf(int leafIndex, RayData* rayData, float* maximumT)
            {
                if (Handler.AllowTest(leafIndex))
                {
                    ref var child = ref Children[leafIndex];
                    CompoundChildShapeTester tester;
                    tester.T = -1;
                    tester.Normal = default;
                    Shapes[child.ShapeIndex.Type].RayTest(child.ShapeIndex.Index, child.LocalPose, *rayData, ref *maximumT, ref tester);
                    if (tester.T >= 0)
                    {
                        Debug.Assert(*maximumT >= tester.T, "Whatever generated this ray hit should have obeyed the current maximumT value.");
                        Matrix3x3.Transform(tester.Normal, Orientation, out var rotatedNormal);
                        Handler.OnRayHit(OriginalRay, ref *maximumT, tester.T, rotatedNormal, leafIndex);
                    }
                }
            }
        }

        public void RayTest<TRayHitHandler>(in RigidPose pose, in RayData ray, ref float maximumT, Shapes shapes, ref TRayHitHandler hitHandler) where TRayHitHandler : struct, IShapeRayHitHandler
        {
            Matrix3x3.CreateFromQuaternion(pose.Orientation, out var orientation);
            Matrix3x3.TransformTranspose(ray.Origin - pose.Position, orientation, out var localOrigin);
            Matrix3x3.TransformTranspose(ray.Direction, orientation, out var localDirection);
            var leafTester = new LeafTester<TRayHitHandler>(Children, shapes, hitHandler, orientation, ray);
            Tree.RayCast(localOrigin, localDirection, ref maximumT, ref leafTester);
            //Copy the hitHandler to preserve any mutation.
            hitHandler = leafTester.Handler;
        }

        public unsafe void RayTest<TRayHitHandler>(in RigidPose pose, ref RaySource rays, Shapes shapes, ref TRayHitHandler hitHandler) where TRayHitHandler : struct, IShapeRayHitHandler
        {
            //TODO: Note that we dispatch a bunch of scalar tests here. You could be more clever than this- batched tests are possible. 
            //May be worth creating a different traversal designed for low ray counts- might be able to get some benefit out of a semidynamic packet or something.
            Matrix3x3.CreateFromQuaternion(pose.Orientation, out var orientation);
            Matrix3x3.Transpose(orientation, out var inverseOrientation);
            var leafTester = new LeafTester<TRayHitHandler>(Children, shapes, hitHandler, orientation, default);
            for (int i = 0; i < rays.RayCount; ++i)
            {
                rays.GetRay(i, out var ray, out var maximumT);
                leafTester.OriginalRay = *ray;
                Matrix3x3.Transform(ray->Origin - pose.Position, inverseOrientation, out var localOrigin);
                Matrix3x3.Transform(ray->Direction, inverseOrientation, out var localDirection);
                Tree.RayCast(localOrigin, localDirection, ref *maximumT, ref leafTester);
            }
            //Preserve any mutations.
            hitHandler = leafTester.Handler;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ShapeBatch CreateShapeBatch(BufferPool pool, int initialCapacity, Shapes shapes)
        {
            return new CompoundShapeBatch<BigCompound>(pool, initialCapacity, shapes);
        }

        public int ChildCount
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { return Children.Length; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref CompoundChild GetChild(int compoundChildIndex)
        {
            return ref Children[compoundChildIndex];
        }

        /// <summary>
        /// Adds a child to the compound.
        /// </summary>
        /// <param name="child">Child to add to the compound.</param>
        /// <param name="pool">Pool to use to resize the compound's children buffer if necessary.</param>
        /// <param name="shapes">Shapes collection containing the compound's children.</param>
        /// <remarks><para>This function keeps the <see cref="Tree"/> in a valid state, but significant changes over time may degrade the tree's quality and result in reduced collision/query performance.
        /// If this happens, consider calling <see cref="Tree.RefitAndRefine(BufferPool, int, float)"/> with a refinementIndex that changes with each call (to prioritize different parts of the tree).
        /// Incrementing a counter with each call would work fine. The ideal frequency of refinement depends on the kind of modifications being made, but it's likely to be rare.</para></remarks>
        public void Add(CompoundChild child, BufferPool pool, Shapes shapes)
        {
            pool.Resize(ref Children, Children.Length + 1, Children.Length);
            Children[^1] = child;
            shapes.UpdateBounds(child.LocalPose, child.ShapeIndex, out var bounds);
            var childIndex = Tree.Add(bounds, pool);
            Debug.Assert(childIndex == Children.Length - 1, "Adding to a tree acts like appending to the list; a newly added element should be in the last slot to match our previous child modification.");
        }

        /// <summary>
        /// Removes a child from the compound by index. The last child is pulled to fill the gap left by the removed child.
        /// </summary>
        /// <param name="childIndex">Index of the child to remove from the compound.</param>
        /// <param name="pool">Pool to use to resize the compound's children buffer if necessary.</param>
        /// <remarks>This function keeps the <see cref="Tree"/> in a valid state, but significant changes over time may degrade the tree's quality and result in reduced collision/query performance.
        /// If this happens, consider calling <see cref="Tree.RefitAndRefine(BufferPool, int, float)"/> with a refinementIndex that changes with each call (to prioritize different parts of the tree).
        /// Incrementing a counter with each call would work fine. The ideal frequency of refinement depends on the kind of modifications being made, but it's likely to be rare.</remarks>
        public void RemoveAt(int childIndex, BufferPool pool)
        {
            var movedChildIndex = Tree.RemoveAt(childIndex);
            if (movedChildIndex >= 0)
            {
                Children[childIndex] = Children[movedChildIndex];
                Debug.Assert(movedChildIndex == Children.Length - 1, "The child moved by the tree is expected to be the last one; it's pulled forward to fill the gap left by the removed child.");
            }
            //Shrinking the buffer takes care of 'removing' the now-empty last slot.
            pool.Resize(ref Children, Children.Length - 1, Children.Length - 1);
        }

        unsafe struct Enumerator<TSubpairOverlaps> : IBreakableForEach<int> where TSubpairOverlaps : ICollisionTaskSubpairOverlaps
        {
            public BufferPool Pool;
            public void* Overlaps;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool LoopBody(int i)
            {
                Unsafe.AsRef<TSubpairOverlaps>(Overlaps).Allocate(Pool) = i;
                return true;
            }
        }

        public unsafe void FindLocalOverlaps<TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
            where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
            where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
        {
            //For now, we don't use anything tricky. Just traverse every child against the tree sequentially.
            //TODO: This sequentializes a whole lot of cache misses. You could probably get some benefit out of traversing all pairs 'simultaneously'- that is, 
            //using the fact that we have lots of independent queries to ensure the CPU always has something to do.
            Enumerator<TSubpairOverlaps> enumerator;
            enumerator.Pool = pool;
            for (int i = 0; i < pairs.Length; ++i)
            {
                ref var pair = ref pairs[i];
                enumerator.Overlaps = Unsafe.AsPointer(ref overlaps.GetOverlapsForPair(i));
                Unsafe.AsRef<BigCompound>(pair.Container).Tree.GetOverlaps(pair.Min, pair.Max, ref enumerator);
            }
        }

        unsafe struct SweepLeafTester<TOverlaps> : ISweepLeafTester where TOverlaps : ICollisionTaskSubpairOverlaps
        {
            public BufferPool Pool;
            public void* Overlaps;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void TestLeaf(int leafIndex, ref float maximumT)
            {
                Unsafe.AsRef<TOverlaps>(Overlaps).Allocate(Pool) = leafIndex;
            }
        }
        public unsafe void FindLocalOverlaps<TOverlaps>(in Vector3 min, in Vector3 max, in Vector3 sweep, float maximumT, BufferPool pool, Shapes shapes, void* overlaps)
            where TOverlaps : ICollisionTaskSubpairOverlaps
        {
            SweepLeafTester<TOverlaps> enumerator;
            enumerator.Pool = pool;
            enumerator.Overlaps = overlaps;
            Tree.Sweep(min, max, sweep, maximumT, ref enumerator);
        }

        /// <summary>
        /// Computes the inertia of a compound. Does not recenter the child poses.
        /// </summary>
        /// <param name="childMasses">Masses of the children.</param>
        /// <param name="shapes">Shapes collection containing the data for the compound child shapes.</param>
        /// <returns>Inertia of the compound.</returns>
        public BodyInertia ComputeInertia(Span<float> childMasses, Shapes shapes)
        {
            return CompoundBuilder.ComputeInertia(Children, childMasses, shapes);
        }

        /// <summary>
        /// Computes the inertia of a compound. Recenters the child poses around the calculated center of mass.
        /// </summary>
        /// <param name="shapes">Shapes collection containing the data for the compound child shapes.</param>
        /// <param name="childMasses">Masses of the children.</param>
        /// <param name="centerOfMass">Calculated center of mass of the compound. Subtracted from all the compound child poses.</param>
        /// <returns>Inertia of the compound.</returns>
        public BodyInertia ComputeInertia(Span<float> childMasses, Shapes shapes, out Vector3 centerOfMass)
        {
            var bodyInertia = CompoundBuilder.ComputeInertia(Children, childMasses, shapes, out centerOfMass);
            //Recentering moves the children around, so the tree needs to be updated.
            Tree.Refit();
            return bodyInertia;
        }

        public void Dispose(BufferPool bufferPool)
        {
            bufferPool.Return(ref Children);
            Tree.Dispose(bufferPool);
        }

        /// <summary>
        /// Type id of compound shapes.
        /// </summary>
        public const int Id = 7;
        public int TypeId { [MethodImpl(MethodImplOptions.AggressiveInlining)] get { return Id; } }
    }


}
