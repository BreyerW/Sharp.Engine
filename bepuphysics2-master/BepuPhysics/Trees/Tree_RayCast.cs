using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading;

namespace BepuPhysics.Trees
{
	partial struct Tree
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe static bool Intersects(in Vector3 min, in Vector3 max, TreeRay* ray, out float t)
		{
			var t0 = min * ray->InverseDirection - ray->OriginOverDirection;
			var t1 = max * ray->InverseDirection - ray->OriginOverDirection;
			var tExit = Vector3.Max(t0, t1);
			var tEntry = Vector3.Min(t0, t1);
			//TODO: Note the use of broadcast and SIMD min/max here. This is much faster than using branches to compute minimum elements, since the branches
			//get mispredicted extremely frequently. Also note 4-wide operations; they're actually faster than using Vector2 or Vector3 due to some unnecessary codegen as of this writing.
			var earliestExit = Vector4.Min(Vector4.Min(new Vector4(ray->MaximumT), new Vector4(tExit.X)), Vector4.Min(new Vector4(tExit.Y), new Vector4(tExit.Z))).X;
			t = Vector4.Max(Vector4.Max(new Vector4(tEntry.X), Vector4.Zero), Vector4.Max(new Vector4(tEntry.Y), new Vector4(tEntry.Z))).X;
			return t <= earliestExit;
		}


		internal readonly unsafe void RayCast<TLeafTester>(int nodeIndex, TreeRay* treeRay, RayData* rayData, int* stack, ref TLeafTester leafTester) where TLeafTester : IRayLeafTester
		{
			Debug.Assert((nodeIndex >= 0 && nodeIndex < nodeCount) || (Encode(nodeIndex) >= 0 && Encode(nodeIndex) < leafCount));
			Debug.Assert(leafCount >= 2, "This implementation assumes all nodes are filled.");

			int stackEnd = 0;
			while (true)
			{
				if (nodeIndex < 0)
				{
					//This is actually a leaf node.
					var leafIndex = Encode(nodeIndex);
					leafTester.TestLeaf(leafIndex, rayData, &treeRay->MaximumT);
					//Leaves have no children; have to pull from the stack to get a new target.
					if (stackEnd == 0)
						return;
					nodeIndex = stack[--stackEnd];
				}
				else
				{
					ref var node = ref Nodes[nodeIndex];
					var aIntersected = Intersects(node.A.Min, node.A.Max, treeRay, out var tA);
					var bIntersected = Intersects(node.B.Min, node.B.Max, treeRay, out var tB);

					if (aIntersected)
					{
						if (bIntersected)
						{
							//Visit the earlier AABB intersection first.
							Debug.Assert(stackEnd < TraversalStackCapacity - 1, "At the moment, we use a fixed size stack. Until we have explicitly tracked depths, watch out for excessive depth traversals.");
							if (tA < tB)
							{
								nodeIndex = node.A.Index;
								stack[stackEnd++] = node.B.Index;
							}
							else
							{
								nodeIndex = node.B.Index;
								stack[stackEnd++] = node.A.Index;
							}
						}
						else
						{
							//Single intersection cases don't require an explicit stack entry.
							nodeIndex = node.A.Index;
						}
					}
					else if (bIntersected)
					{
						nodeIndex = node.B.Index;
					}
					else
					{
						//No intersection. Need to pull from the stack to get a new target.
						if (stackEnd == 0)
							return;
						nodeIndex = stack[--stackEnd];
					}
				}
			}

		}

		internal const int TraversalStackCapacity = 256;

		internal readonly unsafe void RayCast<TLeafTester>(TreeRay* treeRay, RayData* rayData, ref TLeafTester leafTester) where TLeafTester : IRayLeafTester
		{
			if (leafCount == 0)
				return;

			if (leafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (Intersects(Nodes[0].A.Min, Nodes[0].A.Max, treeRay, out var tA))
				{
					leafTester.TestLeaf(0, rayData, &treeRay->MaximumT);
				}
			}
			else
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.
				var stack = stackalloc int[TraversalStackCapacity];
				RayCast(0, treeRay, rayData, stack, ref leafTester);
			}
		}

		public readonly unsafe void RayCast<TLeafTester>(in Vector3 origin, in Vector3 direction, ref float maximumT, ref TLeafTester leafTester, int id = 0) where TLeafTester : IRayLeafTester
		{
			TreeRay.CreateFrom(origin, direction, maximumT, id, out var rayData, out var treeRay);
			RayCast(&treeRay, &rayData, ref leafTester);
			//The maximumT could have been mutated by the leaf tester. Propagate that change. This is important for when we jump between tree traversals and such.
			maximumT = treeRay.MaximumT;
		}
		[StructLayout(LayoutKind.Sequential)]
		struct ExtentsRepresentation
		{
			public Vector3 center;
			private float padding1;
			public Vector3 extents;
			private float padding2;
		}

		//representation of 0b1111_1111_1111_1111_1111_1111_1100_0000 on little endian that also works on big endian
		private const uint isInside = 4294967232;
		private static ConcurrentDictionary<int, int> failedPlane = new();
		private static Buffer<QuickList<int>> leavesToTest = new();
		private static int[] startingIndexes = new int[4];
		private static int startingIndexesLength;
		private static int currentIndex;
		private static int interThreadExchange;
		private static IThreadDispatcher threadDispatcher;
		private static int globalRemainingLeavesToVisit;
		private unsafe static FrustumData* frustumData;

		public unsafe void FrustumSweep<TLeafTester>(FrustumData* frustumData, BufferPool pool, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			if (leafCount == 0)
				return;
			Tree.frustumData = frustumData;
			if (leafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (IntersectsOrInside(Nodes[0].A.Min, Nodes[0].A.Max, frustumData, Nodes[0].A.Index).IsBitSetAt(6))
				{
					leafTester.TestLeaf(0, frustumData);
				}
			}
			else
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.

				var stack = stackalloc int[TraversalStackCapacity];
				FrustumSweep(0, stack, ref leafTester);
			}
		}
		public unsafe void FrustumSweepMultithreaded<TLeafTester>(FrustumData* frustumData, BufferPool pool, ref TLeafTester leafTester, IThreadDispatcher dispatcher) where TLeafTester : IFrustumLeafTester
		{
			if (leafCount == 0)
				return;
			Tree.frustumData = frustumData;
			if (leafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (IntersectsOrInside(Nodes[0].A.Min, Nodes[0].A.Max, frustumData, Nodes[0].A.Index).IsBitSetAt(6))
				{
					leafTester.TestLeaf(0, frustumData);
				}
			}
			else if (leafCount < dispatcher.ThreadCount * 4)
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.

				var stack = stackalloc int[TraversalStackCapacity];
				FrustumSweep(0, stack, ref leafTester);
			}
			else
			{
				FrustumSweepMultithreaded(0, pool, ref leafTester, dispatcher);
			}
		}
		private unsafe void TestAABBs(ref int counter, ref int nodeIndex, ref int leafIndex, ref int fullyContainedStack, ref uint planeBitmask, ref int stackEnd, int* stack)
		{
			if (nodeIndex < 0)
			{
				//This is actually a leaf node.
				leafIndex = Encode(nodeIndex);
				counter--;
				//Leaves have no children; have to pull from the stack to get a new target.
				if (stackEnd == 0)
					return;
				nodeIndex = stack[--stackEnd];

				//check if we are no longer fully inside frustum. if yes reset fullyContainedStack
				if (stackEnd < fullyContainedStack)
					fullyContainedStack = -1;
				//reset bitmask every time when we have to go back and we arent fully inside frustum
				//we must make separate test from previous test because for fullyContainedStack=-1 prev test would never be true
				if (fullyContainedStack < -1)
					planeBitmask = uint.MaxValue;
			}
			else
			{
				ref var node = ref Nodes[nodeIndex];
				//skip tests if frustum fully contains childs,
				//unset bit means fully inside single plane,
				//set bit means intersection with that plane,
				//and 7th least significant bit UNset means that AABB is outside frustum
				//we have six planes thats why we check 6 zeroes
				if (planeBitmask == isInside)
				{
					Debug.Assert(stackEnd < TraversalStackCapacity - 1, "At the moment, we use a fixed size stack. Until we have explicitly tracked depths, watch out for excessive depth traversals.");
					nodeIndex = node.A.Index;
					//make sure leaf never lands on stack
					//this is necessary for multithreaded algorithm
					if (node.B.Index < 0)
					{
						//This is actually a leaf node.
						leafIndex = Encode(node.B.Index);
						counter--;
					}
					else
						stack[stackEnd++] = node.B.Index;
				}
				else
				{
					var aBitmask = IntersectsOrInside(node.A.Min, node.A.Max, frustumData, node.A.Index, planeBitmask);
					var bBitmask = IntersectsOrInside(node.B.Min, node.B.Max, frustumData, node.B.Index, planeBitmask);

					var aIntersected = aBitmask.IsBitSetAt(6);
					var bIntersected = bBitmask.IsBitSetAt(6);

					if (aIntersected && !bIntersected)
					{
						nodeIndex = node.A.Index;
						planeBitmask = aBitmask;

						//One of branches was discarded. Discard all leaves in this branch
						counter -= node.B.LeafCount;
						//check if A child is fully contained in frustum
						//remember we can still intersect at this point and we need to be fully inside
						if (aBitmask == isInside)
							fullyContainedStack = stackEnd;
					}
					else if (aIntersected && bIntersected)
					{
						Debug.Assert(stackEnd < TraversalStackCapacity - 1, "At the moment, we use a fixed size stack. Until we have explicitly tracked depths, watch out for excessive depth traversals.");
						nodeIndex = node.A.Index;
						planeBitmask = aBitmask;
						//check if both childs are fully contained in frustum
						//remember we can still intersect at this point and we need to be fully inside
						if (aBitmask == isInside && bBitmask == isInside)
							fullyContainedStack = stackEnd;

						//make sure leaf never lands on stack
						//this is necessary for multithreaded algorithm
						if (node.B.Index < 0)
						{
							//This is actually a leaf node.
							leafIndex = Encode(node.B.Index);
							counter--;
						}
						else
							stack[stackEnd++] = node.B.Index;
					}
					else if (bIntersected)
					{
						nodeIndex = node.B.Index;
						planeBitmask = bBitmask;

						//One of branches was discarded. Discard all leaves in this branch
						counter -= node.A.LeafCount;
						//check if B child is fully contained in frustum
						//remember we can still intersect at this point and we need to be fully inside
						if (bBitmask == isInside)
							fullyContainedStack = stackEnd;
					}
					else
					{
						//Both branches were discarded. Discard all leaves in these branches
						counter -= node.A.LeafCount + node.B.LeafCount;
						//No intersection. Need to pull from the stack to get a new target.
						if (stackEnd == 0)
							return;
						nodeIndex = stack[--stackEnd];
						//check if we are no longer fully inside frustum. if yes reset fullyContainedStack
						if (stackEnd < fullyContainedStack)
							fullyContainedStack = -1;
						//reset bitmask every time when we have to go back and we arent fully inside frustum
						//we must make separate test from previous test because for fullyContainedStack=-1 prev test would never be true
						if (fullyContainedStack == -1)
							planeBitmask = uint.MaxValue;
					}
				}

			}
		}
		internal unsafe void FrustumSweepMultithreaded<TLeafTester>(int nodeIndex, BufferPool pool, ref TLeafTester leafTester, IThreadDispatcher dispatcher) where TLeafTester : IFrustumLeafTester
		{
			threadDispatcher = dispatcher;
			globalRemainingLeavesToVisit = leafCount;
			currentIndex = -1;
			startingIndexesLength = 0;
			interThreadExchange = 0;
			Debug.Assert((nodeIndex >= 0 && nodeIndex < nodeCount) || (Encode(nodeIndex) >= 0 && Encode(nodeIndex) < leafCount));
			Debug.Assert(leafCount >= 2, "This implementation assumes all nodes are filled.");

			pool.Take(threadDispatcher.ThreadCount, out leavesToTest);

			for (int i = 0; i < threadDispatcher.ThreadCount; i++)
				leavesToTest[i] = new QuickList<int>(leafCount, threadDispatcher.GetThreadMemoryPool(i));

			int multithreadingLeafCountThreshold = leafCount / (threadDispatcher.ThreadCount);
			CollectNodesForMultithreadedCulling(0, multithreadingLeafCountThreshold);
			threadDispatcher.DispatchWorkers(FrustumSweepThreaded);
			for (var i = 0; i < threadDispatcher.ThreadCount; i++)
			{
				foreach (var leafIndex in leavesToTest[i])
					leafTester.TestLeaf(leafIndex, frustumData);
			}
			pool.Return(ref leavesToTest);
		}
		internal unsafe void FrustumSweep<TLeafTester>(int nodeIndex, int* stack, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			Debug.Assert((nodeIndex >= 0 && nodeIndex < nodeCount) || (Encode(nodeIndex) >= 0 && Encode(nodeIndex) < leafCount));
			Debug.Assert(leafCount >= 2, "This implementation assumes all nodes are filled.");
			globalRemainingLeavesToVisit = leafCount;
			uint planeBitmask = uint.MaxValue;
			ref int leafIndex = ref Unsafe.AsRef(-1);
			ref int stackEnd = ref Unsafe.AsRef(0);
			ref int fullyContainedStack = ref Unsafe.AsRef(-1);
			while (true)
			{
				TestAABBs(ref globalRemainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
				if (leafIndex > -1)
				{
					leafTester.TestLeaf(leafIndex, frustumData);
					leafIndex = -1;
				}
				if (globalRemainingLeavesToVisit is 0)
					return;
			}
		}
		private unsafe void FrustumSweepThreaded(int workerIndex)
		{
			var stack = stackalloc int[TraversalStackCapacity];
			ref var node = ref Nodes[0];
			var remainingLeavesToVisit = 0;
			var takenLeavesCount = 0;
			ref int nodeIndex = ref Unsafe.AsRef(0);
			uint planeBitmask = uint.MaxValue;
			ref int leafIndex = ref Unsafe.AsRef(-1);
			ref int stackEnd = ref Unsafe.AsRef(0);
			ref int fullyContainedStack = ref Unsafe.AsRef(-1);
			var givenUpCount = 0;
			while (true)
			{
				if (leafIndex > -1)
				{
					Debug.Assert(leavesToTest[workerIndex].Contains(leafIndex) is false, "Duplicates are unacceptable");
					leavesToTest[workerIndex].AddUnsafely(leafIndex);
					leafIndex = -1;
				}

				if (remainingLeavesToVisit is 0)
				{
					Interlocked.Add(ref globalRemainingLeavesToVisit, -takenLeavesCount);
					nodeIndex = 0;
					//we no longer have work on this thread. We try to steal some from other threads
					if (currentIndex < startingIndexesLength)
					{
						var nextExtraStartingIndex = Interlocked.Increment(ref currentIndex);
						//due to multithreading it might happen that 2 threads attempt to increment
						//while currentIndex is smaller by 1 than startingIndexesLength
						//resulting in error if we dont check again
						if (nextExtraStartingIndex < startingIndexesLength)
							nodeIndex = startingIndexes[nextExtraStartingIndex];
					}

					while (nodeIndex is 0 && globalRemainingLeavesToVisit > 0)
						nodeIndex = Interlocked.Exchange(ref interThreadExchange, 0);
					if (nodeIndex != 0)
					{
						if (nodeIndex < 0)
						{
							nodeIndex = -nodeIndex;
							planeBitmask = isInside;
						}
						else
							planeBitmask = uint.MaxValue;
						node = ref Nodes[nodeIndex];
						remainingLeavesToVisit = node.A.LeafCount + node.B.LeafCount;
						takenLeavesCount = remainingLeavesToVisit;
					}
					else return;
					leafIndex = -1;
					fullyContainedStack = -1;
					givenUpCount = 0;
				}
				else if (globalRemainingLeavesToVisit > 0 && stackEnd > 0)
				{
					//We actively give up one branch when other thread stole it,	
					//to simplify interthread communication
					//provided we have anything to give up
					//We give up branch at the bottom not at the top
					//because bottom branch usually has the most amount of leaves left to check

					var index = stack[0];
					var oldVal = Interlocked.CompareExchange(ref interThreadExchange, fullyContainedStack is 0 ? -index : index, 0);

					if (oldVal is 0)
					{
						stackEnd--;
						node = ref Nodes[index];
						remainingLeavesToVisit -= (node.A.LeafCount + node.B.LeafCount);
						takenLeavesCount -= (node.A.LeafCount + node.B.LeafCount);
						if (fullyContainedStack > 0)
							fullyContainedStack--;
						if (stackEnd == 0)
						{
							stack = (int*)Unsafe.Subtract<int>(stack, givenUpCount);
							givenUpCount = 0;
						}
						else
						{
							stack = (int*)Unsafe.Add<int>(stack, 1);
							givenUpCount++;
						}
					}
				}
				TestAABBs(ref remainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
			}
		}
		unsafe void CollectNodesForMultithreadedCulling(int nodeIndex, int leafCountThreshold)
		{
			ref var node = ref Nodes[nodeIndex];

			if (node.A.Index > 0)
				if (node.A.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(node.A.Index, leafCountThreshold);
				}
				else
				{
					startingIndexes[startingIndexesLength] = node.A.Index;
					startingIndexesLength++;
					if (startingIndexesLength == startingIndexes.Length)
						Array.Resize(ref startingIndexes, startingIndexes.Length * 2);
				}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(node.A.Min, node.A.Max, frustumData, node.A.Index).IsBitSetAt(6))
					leavesToTest[0].AddUnsafely(Encode(node.A.Index));
				globalRemainingLeavesToVisit--;
			}
			if (node.B.Index > 0)
				if (node.B.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(node.B.Index, leafCountThreshold);
				}
				else
				{
					startingIndexes[startingIndexesLength] = node.B.Index;
					startingIndexesLength++;
					if (startingIndexesLength == startingIndexes.Length)
						Array.Resize(ref startingIndexes, startingIndexes.Length * 2);
				}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(node.B.Min, node.B.Max, frustumData, node.B.Index).IsBitSetAt(6))
					leavesToTest[0].AddUnsafely(Encode(node.B.Index));
				globalRemainingLeavesToVisit--;
			}
		}
		//TODO: drop far plane, that gives us 15 floats meaning only 16th float is dead
		//or keep far plane and simply flip near plane after computation
		public unsafe static uint IntersectsOrInside(in Vector3 min, in Vector3 max, FrustumData* frustumData, int nodeIndex, uint planeBitmask = uint.MaxValue)
		{
			var shouldRenumberPlanes = failedPlane.TryGetValue(nodeIndex, out var planeId);

			var start = 0;
			//far plane test can be eliminated by setting end=5
			//and changing all occurences of start==5 to start==4 instead.
			//This results in frustum with "infinite" length

			var end = 6;

			//Convert AABB to center-extents representation
			//On NET 5+ can skip conversion and instead use Vector.ConditionalSelect & Vector.GreaterThan with Vector128
			//and use p, n-vertex optimization
			var eRep = new ExtentsRepresentation()
			{
				center = max + min, // Compute AABB center
				extents = max - min // Compute positive extents
			};
			ref var planeAddr = ref Unsafe.As<float, Plane>(ref frustumData->nearPlane.Normal.X);
			ref var plane = ref Unsafe.As<float, Plane>(ref frustumData->nearPlane.Normal.X);

			if (shouldRenumberPlanes)
			{
				start = planeId;
				end = start;
			}

			do
			{
				if (!planeBitmask.IsBitSetAt(start))
				{
					start = shouldRenumberPlanes && start == 5 ? 0 : start + 1;
					continue;
				}
				plane = ref Unsafe.Add(ref planeAddr, start * 2);
				/*var eRep = new ExtentsRepresentation()
				{
					center = new Vector3(), // Compute AABB center
					extents = new Vector3() // Compute positive extents
				};*/
				var d = plane.D;
				float m, r;
				//Vector<float>.Count == 4 is not worth it since built-in VectorX and Plane are already vectorized
				//and for Vector<float>.Count == 16 we should fuse A & B into one test
				//and that will require sperate method with different signature and NET 5+ since Vector512 is required
				if (Vector.IsHardwareAccelerated && Vector<float>.Count == 8)
				{
					//Vector.ConditionalSelect<float>();
					ref Vector<float> planeData = ref Unsafe.As<Plane, Vector<float>>(ref plane);
					ref Vector<float> bbData = ref Unsafe.As<ExtentsRepresentation, Vector<float>>(ref eRep);
					var multi = bbData * planeData;
					m = multi[0] + multi[1] + multi[2];
					r = multi[4] + multi[5] + multi[6];
					//var condition = Vector.GreaterThanOrEqual(plane.Normal, Vector<float>.Zero);
				}
				else
				{
					m = Vector3.Dot(plane.Normal, eRep.center);
					plane = ref Unsafe.Add(ref plane, 1);//absolute normal
					r = Vector3.Dot(plane.Normal, eRep.extents);
				}
				if (m + r < d)//outside
				{
					planeBitmask = planeBitmask.UnsetBitAt(6);
					//no need to renumber planes when id is 0
					if (!shouldRenumberPlanes && start != 0 && start != planeId)
						failedPlane.TryAdd(nodeIndex, start);
					else if (start != 0 && start != planeId)
						failedPlane.TryUpdate(nodeIndex, start, start);
					return planeBitmask;
				}
				if (m - r >= d)//inside
				{
					planeBitmask = planeBitmask.UnsetBitAt(start);
				}
				/*else//intersect
				{

				}*/
				start = shouldRenumberPlanes && start == 5 ? 0 : start + 1;
			} while (start != end);

			if (shouldRenumberPlanes)
				failedPlane.TryRemove(nodeIndex, out _);

			return planeBitmask;
		}
	}
	static class BitExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBitSetAt(this uint mask, int index)
		{
			return ((mask >> index) & 1) == 1;
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SetBitAt(this uint mask, int index)
		{
			return mask | ((uint)1 << index);
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint UnsetBitAt(this uint mask, int index)
		{
			return mask & ~((uint)1 << index);
		}
	}

}
