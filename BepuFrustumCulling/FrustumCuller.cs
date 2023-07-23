using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BepuFrustumCulling
{

	public struct DefaultSweepDataGetter : ISweepDataGetter
	{
		public long TotalLeafCount => FrustumCuller.FrozenTree.LeafCount;

		public ref Tree GetTree(int index)
		{
			if (index is 0)
				return ref FrustumCuller.FrozenTree;
			else
				return ref Unsafe.NullRef<Tree>();
		}

		public ref Buffer<CollidableReference> GetLeaves(int index)
		{
			if (index is 0)
				return ref FrustumCuller.FrozenLeaves;
			else
				return ref Unsafe.NullRef<Buffer<CollidableReference>>();
		}
	}
	public interface ISweepDataGetter
	{
		public long TotalLeafCount { get; }
		public ref Tree GetTree(int index);
		public ref Buffer<CollidableReference> GetLeaves(int index);

	}
	[StructLayout(LayoutKind.Sequential)]
	struct TwoInts
	{
		public int lower;
		public int upper;
	}
	public static class FrustumCuller
	{
		//TODO: change this to be treeCount based?
		internal static QuickList<(long, int)>[] newFailedPlanes = new QuickList<(long, int)>[1];
		internal static BufferPool Pool;
		//representation of 0b1111_1111_1111_1111_1111_1111_1100_0000 on little endian that also works on big endian
		internal const int TraversalStackCapacity = 256;
		internal const uint isInside = 4294967232;
		internal static int largestTreeIndex;
		//TODO: replace with QuickDictionary?
		internal static Dictionary<long, int> failedPlane = new();
		//internal static QuickDictionary<long, int, PrimitiveComparer<long>> failedPlane = new();
		internal static long[] startingIndexes = new long[4];
		internal static int startingIndexesLength;
		internal static int currentIndex;
		internal static int currentTreeId;
		internal static long interThreadExchange;
		internal static IThreadDispatcher threadDispatcher;
		internal static long globalRemainingLeavesToVisit;
		internal unsafe static FrustumData* frustumData;
		internal static Tree.RefitAndRefineMultithreadedContext frozenRefineContext;
		internal static int frameIndex;
		internal static int remainingJobCount;
		internal static Action<int> executeRefitAndMarkAction, executeRefineAction;
		/// <summary>
		/// When total leaf count is higher than this parameter, algorithm switches to multithreading
		/// </summary>
		public static long singleToMultithreadedThreshold = 16;
		/// <summary>
		/// Remove given amount from tree's leaf count and use resulting threshold to gather starting nodes for multithreading. The higher this parameter the more starting nodes will be gathered.
		/// </summary>
		/// <remarks>
		/// Should be strongly less than <see cref="singleToMultithreadedThreshold"/>
		/// </remarks>
		public static int subtractionToLeafCountThreshold = 4;
		/// <summary>
		/// A tree that is like static but doesnt generate any collisions in narrow phase. It must be explicitly queried for collisions. Useful for foliage and other non-interactive objects.
		/// </summary>
		public static Tree FrozenTree;
		/// <summary>
		/// Collection of leafs for <see cref="FrozenTree"/>
		/// </summary>
		public static Buffer<CollidableReference> FrozenLeaves;
		//TODO: add [UnscopedAttribute] Once Bepu moves to net 7+ so that this struct can be directly treated as fixed array of items
		[StructLayout(LayoutKind.Sequential)]
		internal readonly record struct FixedArrayOfItems<T> where T : struct
		{
			public readonly T Item1;
			public readonly T Item2;
			public readonly T Item3;
			public readonly T Item4;
			public readonly T Item5;
			public readonly T Item6;

			public FixedArrayOfItems(T item1, T item2, T item3, T item4, T item5, T item6)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
				Item4 = item4;
				Item5 = item5;
				Item6 = item6;
			}
		}
		internal static FixedArrayOfItems<FixedArrayOfItems<int>> lookUpTable = new(
			new(0, 1, 2, 3, 4, 5),
			new(1, 2, 3, 4, 5, 0),
			new(2, 3, 4, 5, 0, 1),
			new(3, 4, 5, 0, 1, 2),
			new(4, 5, 0, 1, 2, 3),
			new(5, 0, 1, 2, 3, 4)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int Add(CollidableReference collidable, ref BoundingBox bounds, ref Tree tree, BufferPool pool, ref Buffer<CollidableReference> leaves)
		{
			var leafIndex = tree.Add(bounds, pool);
			if (leafIndex >= leaves.Length)
			{
				pool.ResizeToAtLeast(ref leaves, tree.LeafCount + 1, leaves.Length);
			}
			leaves[leafIndex] = collidable;
			return leafIndex;
		}
		public static void CompactMemoryOfFailedPlanes()
		{
			//newFailedPlanesa
		}
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int AddFrozen(CollidableReference collidable, ref BoundingBox bounds)
		{
			return Add(collidable, ref bounds, ref FrozenTree, Pool, ref FrozenLeaves);
		}
		public static FrustumCuller<DefaultSweepDataGetter> Create(BufferPool pool, int initialInactiveLeafCapacity = 4096)
		{
			return Create<DefaultSweepDataGetter>(pool, initialInactiveLeafCapacity);
		}
		public static FrustumCuller<T> Create<T>(BufferPool pool, int initialInactiveLeafCapacity = 4096)
			where T : struct, ISweepDataGetter
		{
			Pool = pool;
			FrozenTree = new Tree(pool, initialInactiveLeafCapacity);
			pool.TakeAtLeast(initialInactiveLeafCapacity, out FrozenLeaves);
			frozenRefineContext = new Tree.RefitAndRefineMultithreadedContext();
			executeRefitAndMarkAction = ExecuteRefitAndMark;
			executeRefineAction = ExecuteRefine;
			return new FrustumCuller<T>();
		}
		static void ExecuteRefitAndMark(int workerIndex)
		{
			var threadPool = threadDispatcher.GetThreadMemoryPool(workerIndex);
			while (true)
			{
				var jobIndex = Interlocked.Decrement(ref remainingJobCount);
				if (jobIndex < 0)
					break;
				Debug.Assert(jobIndex >= 0 && jobIndex < frozenRefineContext.RefitNodes.Count);
				frozenRefineContext.ExecuteRefitAndMarkJob(threadPool, workerIndex, jobIndex);
			}
		}

		static void ExecuteRefine(int workerIndex)
		{
			var threadPool = threadDispatcher.GetThreadMemoryPool(workerIndex);
			var maximumSubtrees = frozenRefineContext.MaximumSubtrees;
			var subtreeReferences = new QuickList<int>(maximumSubtrees, threadPool);
			var treeletInternalNodes = new QuickList<int>(maximumSubtrees, threadPool);
			Tree.CreateBinnedResources(threadPool, maximumSubtrees, out var buffer, out var resources);
			while (true)
			{
				var jobIndex = Interlocked.Decrement(ref remainingJobCount);
				if (jobIndex < 0)
					break;
				Debug.Assert(jobIndex >= 0 && jobIndex < frozenRefineContext.RefinementTargets.Count);
				frozenRefineContext.ExecuteRefineJob(ref subtreeReferences, ref treeletInternalNodes, ref resources, threadPool, jobIndex);
			}
			subtreeReferences.Dispose(threadPool);
			treeletInternalNodes.Dispose(threadPool);
			threadPool.Return(ref buffer);
		}
		public static void Update()
		{
			if (frameIndex == int.MaxValue)
				frameIndex = 0;
			if (threadDispatcher != null)
			{
				frozenRefineContext.CreateRefitAndMarkJobs(ref FrozenTree, Pool, threadDispatcher);
				remainingJobCount = frozenRefineContext.RefitNodes.Count;
				threadDispatcher.DispatchWorkers(executeRefitAndMarkAction, remainingJobCount);
				frozenRefineContext.CreateRefinementJobs(Pool, frameIndex, 1f);
				remainingJobCount = frozenRefineContext.RefinementTargets.Count;
				threadDispatcher.DispatchWorkers(executeRefineAction, remainingJobCount);
				frozenRefineContext.CleanUpForRefitAndRefine(Pool);
			}
			else
			{
				FrozenTree.RefitAndRefine(Pool, frameIndex);
			}
			++frameIndex;
		}
	}


	public class FrustumCuller<T> where T : struct, ISweepDataGetter
	{
		internal static int treeCount;
		static QuickList<int>[] leavesToTest;
		private static T dataGetter;
		private static T DataGetter
		{
			set
			{
				ref Tree tree = ref value.GetTree(treeCount);
				while (Unsafe.IsNullRef(ref tree) is false)
					tree = ref value.GetTree(++treeCount);
				leavesToTest = new QuickList<int>[treeCount];
			}
		}
		static FrustumCuller()
		{
			DataGetter = default;
		}
		/// <summary>
		/// Finds any intersections between a frustum and leaf bounding boxes.
		/// </summary>
		/// <typeparam name="TFrustumTester">Type of the callback to execute on frustum-leaf bounding box intersections.</typeparam>
		/// <param name="matrix">should be multiply of inversed view matrix (or camera's model matrix) and projection matrix </param>
		/// <param name="frustumTester">Callback to execute on frustum-leaf bounding box intersections.</param>
		/// <param name="columnMajor">matrix is column-major or not</param>
		/// <param name="id">User specified id of the frustum.</param>
		public unsafe void FrustumSweep<TFrustumTester>(in Matrix4x4 matrix, ref TFrustumTester frustumTester, bool columnMajor = false, int id = 0, IThreadDispatcher dispatcher = null) where TFrustumTester : struct, IBroadPhaseFrustumTester
		{
			if (dispatcher.ThreadCount > FrustumCuller.newFailedPlanes.Length)
				FrustumCuller.newFailedPlanes = new QuickList<(long, int)>[dispatcher.ThreadCount];
			//TODO: maybe use preprocessor directive instead of bool?
			//#if COLUMNMAJOR
			var frustumData = new FrustumData()
			{
				Id = id
			};
			var planeSpan = new Span<Plane>(&frustumData.nearPlane, 6);
			var vectors = new Span<Vector3>(&frustumData.conditionNearPlane, 6);
			if (columnMajor)
			{
				ref var refMat = ref Unsafe.AsRef(matrix);

				ref var lastColumn = ref Unsafe.As<float, Vector4>(ref refMat.M41);
				ref var firstColumn = ref Unsafe.As<float, Vector4>(ref refMat.M11);
				ref var secondColumn = ref Unsafe.As<float, Vector4>(ref refMat.M21);
				ref var thirdColumn = ref Unsafe.As<float, Vector4>(ref refMat.M31);

				// Near clipping plane
				planeSpan[0] = (new Plane(lastColumn + thirdColumn));

				// Top clipping plane
				planeSpan[1] = (new Plane(lastColumn - secondColumn));

				// Bottom clipping plane
				planeSpan[2] = (new Plane(lastColumn + secondColumn));

				// Left clipping plane
				planeSpan[3] = (new Plane(lastColumn + firstColumn));

				// Right clipping plane
				planeSpan[4] = (new Plane(lastColumn - firstColumn));

				// Far clipping plane
				planeSpan[5] = (new Plane(lastColumn - thirdColumn));

			}
			else
			{
				var lastColumn = new Vector4(matrix.M14, matrix.M24, matrix.M34, matrix.M44);
				var firstColumn = new Vector4(matrix.M11, matrix.M21, matrix.M31, matrix.M41);
				var secondColumn = new Vector4(matrix.M12, matrix.M22, matrix.M32, matrix.M42);
				var thirdColumn = new Vector4(matrix.M13, matrix.M23, matrix.M33, matrix.M43);

				// Near clipping plane
				planeSpan[0] = (new Plane(thirdColumn));

				// Top clipping plane
				planeSpan[1] = (new Plane(lastColumn - secondColumn));

				// Bottom clipping plane
				planeSpan[2] = (new Plane(lastColumn + secondColumn));

				// Left clipping plane
				planeSpan[3] = (new Plane(lastColumn + firstColumn));

				// Right clipping plane
				planeSpan[4] = (new Plane(lastColumn - firstColumn));

				// Far clipping plane
				planeSpan[5] = (new Plane(lastColumn - thirdColumn));

			}
			int c = 0;
			while (c < 6)
			{
				var normal = planeSpan[c].Normal;
				vectors[c] = new Vector3(normal.X > 0 ? 1f : 0, normal.Y > 0 ? 1f : 0, normal.Z > 0 ? 1f : 0);
				c++;
			}
			Unsafe.SkipInit(out FrustumLeafTester<TFrustumTester> tester);
			tester.LeafTester = frustumTester;
			FrustumCuller.frustumData = &frustumData;
			FrustumSweepMultithreaded(0, FrustumCuller.Pool, ref tester, dispatcher);
			//The sweep tester probably relies on mutation to function; copy any mutations back to the original reference.
			//frustumTester = tester.LeafTester;
		}
		private static unsafe void TestAABBs(int treeId, int failedPlanesIndex, ref long counter, ref int nodeIndex, ref int leafIndex, ref int fullyContainedStack, ref uint planeBitmask, ref int stackEnd, int* stack)
		{
			if (nodeIndex < 0)
			{
				//This is actually a leaf node.
				leafIndex = Tree.Encode(nodeIndex);
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
				ref var tree = ref dataGetter.GetTree(treeId);
				ref var node = ref tree.Nodes[nodeIndex];
				//skip tests if frustum fully contains childs,
				//unset bit means fully inside single plane,
				//set bit means intersection with that plane,
				//and 7th least significant bit UNset means that AABB is outside frustum
				//we have six planes thats why we check 6 zeroes
				if (planeBitmask == FrustumCuller.isInside)
				{
					Debug.Assert(stackEnd < FrustumCuller.TraversalStackCapacity - 1, "At the moment, we use a fixed size stack. Until we have explicitly tracked depths, watch out for excessive depth traversals.");
					nodeIndex = node.A.Index;
					//make sure leaf never lands on stack
					//this is necessary for multithreaded algorithm
					if (node.B.Index < 0)
					{
						//This is actually a leaf node.
						leafIndex = Tree.Encode(node.B.Index);
					}
					else
						stack[stackEnd++] = node.B.Index;
				}
				else
				{
					var aBitmask = IntersectsOrInside(ref node.A, treeId, failedPlanesIndex, FrustumCuller.frustumData, planeBitmask);
					var bBitmask = IntersectsOrInside(ref node.B, treeId, failedPlanesIndex, FrustumCuller.frustumData, planeBitmask);

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
						if (aBitmask == FrustumCuller.isInside)
							fullyContainedStack = stackEnd;
					}
					else if (aIntersected && bIntersected)
					{
						Debug.Assert(stackEnd < FrustumCuller.TraversalStackCapacity - 1, "At the moment, we use a fixed size stack. Until we have explicitly tracked depths, watch out for excessive depth traversals.");
						nodeIndex = node.A.Index;
						planeBitmask = aBitmask;
						//check if both childs are fully contained in frustum
						//remember we can still intersect at this point and we need to be fully inside
						if (aBitmask == FrustumCuller.isInside && bBitmask == FrustumCuller.isInside)
							fullyContainedStack = stackEnd;

						//make sure leaf never lands on stack
						//this is necessary for multithreaded algorithm
						if (node.B.Index < 0)
						{
							//This is actually a leaf node.
							leafIndex = Tree.Encode(node.B.Index);
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
						if (bBitmask == FrustumCuller.isInside)
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
		private static unsafe void FrustumSweepMultithreaded<TLeafTester>(int nodeIndex, BufferPool pool, ref TLeafTester leafTester, IThreadDispatcher dispatcher) where TLeafTester : IFrustumLeafTester
		{
			FrustumCuller.currentTreeId = 0;
			ref var tree = ref Unsafe.NullRef<Tree>();
			FrustumCuller.globalRemainingLeavesToVisit = dataGetter.TotalLeafCount;
			var stack = stackalloc int[FrustumCuller.TraversalStackCapacity];
			if (dispatcher is null || dataGetter.TotalLeafCount < FrustumCuller.singleToMultithreadedThreshold)
			{
				pool.Take(256, out FrustumCuller.newFailedPlanes[0].Span);
				FrustumCuller.newFailedPlanes[0].Count = 0;
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.
				while (FrustumCuller.currentTreeId < treeCount)
				{
					tree = ref dataGetter.GetTree(FrustumCuller.currentTreeId);
					if (tree.LeafCount is 0)
					{
						FrustumCuller.currentTreeId++;
						continue;
					}

					FrustumSweep(0, stack, ref leafTester);
					FrustumCuller.currentTreeId++;
				}
				FrustumCuller.failedPlane.Clear();
				int i = 0;
				foreach (var (index, planeId) in FrustumCuller.newFailedPlanes[0])
				{
					i++;
					FrustumCuller.failedPlane.Add(index, planeId);
				}
				pool.Return(ref FrustumCuller.newFailedPlanes[0].Span);
				return;
			}

			FrustumCuller.threadDispatcher = dispatcher;
			FrustumCuller.currentIndex = -1;
			FrustumCuller.startingIndexesLength = 0;
			FrustumCuller.interThreadExchange = 0;
#if DEBUG
			Array.Clear(FrustumCuller.startingIndexes);

#endif
			//Debug.Assert((nodeIndex >= 0 && nodeIndex < tree.NodeCount) || (Tree.Encode(nodeIndex) >= 0 && Tree.Encode(nodeIndex) < tree.LeafCount));
			//Debug.Assert(tree.LeafCount >= 2, "This implementation assumes all nodes are filled.");
			int multithreadingLeafCountThreshold;
			ref var tmp = ref leavesToTest[0];
			for (var i = 0; i < treeCount; i++)
			{
				tree = ref dataGetter.GetTree(i);

				if (tree.LeafCount is 0) continue;
				tmp = ref leavesToTest[i];
				pool.Take(tree.LeafCount, out tmp.Span);
#if DEBUG
				tmp.Clear();
#else
				tmp.Count = 0;
#endif
				multithreadingLeafCountThreshold = tree.LeafCount - FrustumCuller.subtractionToLeafCountThreshold;
				multithreadingLeafCountThreshold = multithreadingLeafCountThreshold < 3 ? tree.LeafCount + 1 : multithreadingLeafCountThreshold;

				CollectNodesForMultithreadedCulling(ref tree.Nodes, i, 0, multithreadingLeafCountThreshold);
			}
			if (FrustumCuller.globalRemainingLeavesToVisit > 0)
			{
				FrustumCuller.threadDispatcher.DispatchWorkers(FrustumSweepThreaded);
				for (var i = 0; i < treeCount; i++)
				{
					leafTester.Leaves = dataGetter.GetLeaves(i);
					tmp = ref leavesToTest[i];
					for (var j = 0; j < tmp.Count; j++)
						leafTester.TestLeaf(tmp[j], FrustumCuller.frustumData);
					pool.Return(ref tmp.Span);
				}
				FrustumCuller.failedPlane.Clear();
				for (var i = 0; i < FrustumCuller.threadDispatcher.ThreadCount; i++)
				{
					ref var buffer = ref FrustumCuller.newFailedPlanes[i];
					foreach (var (index, planeId) in buffer)
						FrustumCuller.failedPlane.Add(index, planeId);
					FrustumCuller.threadDispatcher.GetThreadMemoryPool(i).Return(ref buffer.Span);
				}
			}
		}
		private static unsafe void FrustumSweep<TLeafTester>(int nodeIndex, int* stack, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			ref var tree = ref dataGetter.GetTree(FrustumCuller.currentTreeId);
			if (tree.LeafCount == 0)
				return;
			leafTester.Leaves = dataGetter.GetLeaves(FrustumCuller.currentTreeId);
			if (tree.LeafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (IntersectsOrInside(ref tree.Nodes[0].A, FrustumCuller.currentTreeId, 0, FrustumCuller.frustumData).IsBitSetAt(6))
				{
					leafTester.TestLeaf(0, FrustumCuller.frustumData);
				}
				return;
			}

			Debug.Assert((nodeIndex >= 0 && nodeIndex < tree.NodeCount) || (Tree.Encode(nodeIndex) >= 0 && Tree.Encode(nodeIndex) < tree.LeafCount));
			//Debug.Assert(tree.LeafCount >= 2, "This implementation assumes all nodes are filled.");
			uint planeBitmask = uint.MaxValue;
			ref int leafIndex = ref Unsafe.AsRef(-1);
			ref int stackEnd = ref Unsafe.AsRef(0);
			ref int fullyContainedStack = ref Unsafe.AsRef(-1);

			while (true)
			{
				TestAABBs(FrustumCuller.currentTreeId, 0, ref FrustumCuller.globalRemainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
				if (leafIndex > -1)
				{
					leafTester.TestLeaf(leafIndex, FrustumCuller.frustumData);
					leafIndex = -1;
					FrustumCuller.globalRemainingLeavesToVisit--;
				}
				if (FrustumCuller.globalRemainingLeavesToVisit is 0)
					return;
			}
		}
		private static unsafe void FrustumSweepThreaded(int workerIndex)
		{
			FrustumCuller.threadDispatcher.GetThreadMemoryPool(workerIndex).Take(256, out FrustumCuller.newFailedPlanes[workerIndex].Span);
			FrustumCuller.newFailedPlanes[workerIndex].Count = 0;
			var treeId = 0;
			var stack = stackalloc int[FrustumCuller.TraversalStackCapacity];
			var stackStart = &stack[0];
			ref int nodeIndex = ref Unsafe.AsRef(0);
			ref var tree = ref dataGetter.GetTree(treeId);
			ref var node = ref tree.Nodes[nodeIndex];

			long remainingLeavesToVisit = 0;
			var takenLeavesCount = remainingLeavesToVisit;
			uint planeBitmask = uint.MaxValue;
			ref int leafIndex = ref Unsafe.AsRef(-1);
			ref int stackEnd = ref Unsafe.AsRef(0);
			ref int fullyContainedStack = ref Unsafe.AsRef(-1);
			while (true)
			{
				if (leafIndex > -1)
				{
					ref var tmp = ref leavesToTest[treeId];
#if DEBUG
					if (leafIndex is not 0)
						Debug.Assert(tmp.Contains(leafIndex) is false, "Duplicates are unacceptable");
#endif
					//Count check not needed here since backing buffer always has enough space to store whole tree if needed
					var i = Interlocked.Increment(ref tmp.Count);
					tmp[i - 1] = leafIndex;
					leafIndex = -1;
					remainingLeavesToVisit--;
				}
				if (remainingLeavesToVisit is 0)
				{
					nodeIndex = 0;
					Interlocked.Add(ref FrustumCuller.globalRemainingLeavesToVisit, -takenLeavesCount);
					if (FrustumCuller.currentIndex < FrustumCuller.startingIndexesLength)
					{
						var nextStartingIndex = Interlocked.Increment(ref FrustumCuller.currentIndex);
						//due to multithreading it might happen that 2 or more threads attempt to increment
						//while currentIndex is smaller by 1 than startingIndexesLength
						//resulting in error if we dont check again
						if (nextStartingIndex < FrustumCuller.startingIndexesLength)
						{
							ref var tmp = ref Unsafe.As<long, TwoInts>(ref FrustumCuller.startingIndexes[nextStartingIndex]);
							treeId = tmp.lower;
							nodeIndex = tmp.upper;
						}
					}
					if (nodeIndex is 0)
					{
						//we no longer have work on this thread. We try to steal some from other threads
						while (FrustumCuller.globalRemainingLeavesToVisit > 0 && nodeIndex is 0)
						{
							var backstore = Interlocked.Exchange(ref FrustumCuller.interThreadExchange, 0);
							ref var tmp = ref Unsafe.As<long, TwoInts>(ref backstore);
							nodeIndex = tmp.lower;
							treeId = tmp.upper;
						}
						if (nodeIndex is not 0)
						{
							//If less than 0 that means stolen index is fully inside frustum,
							//saving us some work from testing all planes on this index
							if (nodeIndex < 0)
							{
								nodeIndex = -nodeIndex;
								planeBitmask = FrustumCuller.isInside;
							}
							else
								planeBitmask = uint.MaxValue;
						}
						else return;
					}
					stackEnd = 0;
					stack = stackStart;
					tree = ref dataGetter.GetTree(treeId);
					node = ref tree.Nodes[nodeIndex];
					remainingLeavesToVisit = node.A.LeafCount + node.B.LeafCount;
					takenLeavesCount = remainingLeavesToVisit;
					leafIndex = -1;
					fullyContainedStack = -1;
				}
				else if (FrustumCuller.globalRemainingLeavesToVisit > 0 && stackEnd > 0)
				{
					//We actively give up one branch which is not just a leaf when other thread stole it,	
					//to simplify interthread communication
					//provided we have anything to give up
					//We give up branch at the bottom of stack not at the top
					//because bottom branch usually has the most amount of leaves left to check

					var index = *stack;
					long interThreadExchangeData = 0;
					ref var tmp = ref Unsafe.As<long, TwoInts>(ref interThreadExchangeData);
					tmp.lower = fullyContainedStack is 0 ? -index : index;
					tmp.upper = treeId;
					var oldVal = Interlocked.CompareExchange(ref FrustumCuller.interThreadExchange, interThreadExchangeData, 0);

					if (oldVal is 0)
					{
						stackEnd--;
						node = ref tree.Nodes[index];
						remainingLeavesToVisit -= (node.A.LeafCount + node.B.LeafCount);
						takenLeavesCount -= (node.A.LeafCount + node.B.LeafCount);
						if (fullyContainedStack > 0)
							fullyContainedStack--;
						if (stackEnd == 0)
						{
							stack = stackStart;
						}
						else
						{
							stack = (int*)Unsafe.Add<int>(stack, 1);
						}
					}
				}
				//debug.Add((nodeIndex, workerIndex));
				TestAABBs(treeId, workerIndex, ref remainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
			}
		}
		static unsafe void CollectNodesForMultithreadedCulling(ref Buffer<Node> Nodes, int treeId, int nodeIndex, int leafCountThreshold)
		{
			ref var node = ref Nodes[nodeIndex];

			if (node.A.Index > 0)
			{
				if (node.A.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(ref Nodes, treeId, node.A.Index, leafCountThreshold);
				}
				else
				{
					var packed = (long)node.A.Index << 32 | (uint)treeId;
					Debug.Assert(FrustumCuller.startingIndexes.Contains(packed) is false, "Duplicates are unacceptable");
					FrustumCuller.startingIndexes[FrustumCuller.startingIndexesLength] = (long)node.A.Index << 32 | (uint)treeId;// node.A.Index;
					FrustumCuller.startingIndexesLength++;
					if (FrustumCuller.startingIndexesLength == FrustumCuller.startingIndexes.Length)
						Array.Resize(ref FrustumCuller.startingIndexes, FrustumCuller.startingIndexes.Length * 2);
				}
			}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(ref node.A, treeId, 0, FrustumCuller.frustumData).IsBitSetAt(6))
					leavesToTest[treeId].AddUnsafely(Tree.Encode(node.A.Index));
				FrustumCuller.globalRemainingLeavesToVisit--;
			}
			if (node.B.Index > 0)
			{
				if (node.B.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(ref Nodes, treeId, node.B.Index, leafCountThreshold);
				}
				else
				{
					var packed = (long)node.B.Index << 32 | (uint)treeId;
					Debug.Assert(FrustumCuller.startingIndexes.Contains(packed) is false, "Duplicates are unacceptable");
					FrustumCuller.startingIndexes[FrustumCuller.startingIndexesLength] = (long)node.B.Index << 32 | (uint)treeId;
					FrustumCuller.startingIndexesLength++;
					if (FrustumCuller.startingIndexesLength == FrustumCuller.startingIndexes.Length)
						Array.Resize(ref FrustumCuller.startingIndexes, FrustumCuller.startingIndexes.Length * 2);
				}
			}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(ref node.B, treeId, 0, FrustumCuller.frustumData).IsBitSetAt(6))
					leavesToTest[treeId].AddUnsafely(Tree.Encode(node.B.Index));
				FrustumCuller.globalRemainingLeavesToVisit--;
			}
		}
		//TODO: drop far plane, that gives us 15 floats meaning only 16th float is dead
		//or keep far plane and simply flip near plane after computation
		//TODO: make version with A and B nodes as parameters to fuse their testing instead of min/max vectors and index 
		private unsafe static uint IntersectsOrInside(ref NodeChild node, int treeId, int failedPlanesIndex, FrustumData* frustumData, uint planeBitmask = uint.MaxValue)
		{
			var packedIndex = (long)node.Index << 32 | (uint)treeId;
			var shouldRenumberPlanes = FrustumCuller.failedPlane.TryGetValue(packedIndex, out var planeId);

			//Convert AABB to center-extents representation
			//On NET 5+ can skip conversion and instead use Vector.ConditionalSelect & Vector.GreaterThan with Vector128

			ref var planeAddr = ref frustumData->nearPlane;
			ref var plane = ref frustumData->nearPlane;
			ref var conditionAddr = ref frustumData->conditionNearPlane;
			//far plane test can be eliminated by modyfying lookuptable
			//from 6x6 to 5x5 and deleting every occurance of 5 in table
			//This results in frustum with "infinite" length
			ref var indexesToVisit = ref Unsafe.Add(ref Unsafe.AsRef(FrustumCuller.lookUpTable.Item1), planeId);
			ref var end = ref Unsafe.AsRef(indexesToVisit.Item6);
			ref var id = ref Unsafe.AsRef(indexesToVisit.Item1);
			//TODO: change to Unsafe.IsAddressLessThanOrEqual(ref id, ref end) on NET 8
			while (!Unsafe.IsAddressLessThan(ref end, ref id))
			{
				if (!planeBitmask.IsBitSetAt(id))
				{
					id = ref Unsafe.Add(ref id, 1);
					continue;
				}
				plane = ref Unsafe.Add(ref planeAddr, id);
				var condition = Unsafe.Add(ref conditionAddr, id);
				var reverseCondition = Vector3.One - condition;
				var d = plane.D;
				float m, r;
				//Vector<float>.Count == 4 is not worth it since built-in VectorX and Plane are already vectorized
				//and for Vector<float>.Count == 16 we should fuse A & B into one test
				//and that will require sperate method with different signature and NET 5+ since Vector512 is required
				/*if (Vector.IsHardwareAccelerated && Vector<float>.Count == 8)
				{
					//Vector.ConditionalSelect<float>();
					ref Vector<float> planeData = ref Unsafe.As<Plane, Vector<float>>(ref plane);
					//ref Vector<float> bbData = ref Unsafe.As<ExtentsRepresentation, Vector<float>>(ref eRep);
					var multi = bbData * planeData;
					m = multi[0] + multi[1] + multi[2];
					r = multi[4] + multi[5] + multi[6];
					//var condition = Vector.GreaterThanOrEqual(plane.Normal, Vector<float>.Zero);
				}
				else*/
				{
					var n = node.Min * condition + node.Max * reverseCondition;
					var p = node.Max * condition + node.Min * reverseCondition;

					m = Vector3.Dot(n, plane.Normal);
					//plane = ref Unsafe.Add(ref plane, 1);//absolute normal
					r = Vector3.Dot(p, plane.Normal);
				}

				if (r < -d)//outside
				{
					planeBitmask = planeBitmask.UnsetBitAt(6);
					//no need to renumber planes when id is 0
					if (id != 0)
					{
						ref var buffer = ref FrustumCuller.newFailedPlanes[failedPlanesIndex];
						buffer.AddUnsafely((packedIndex, id));
					}
					return planeBitmask;
				}
				if (m < -d)//intersect
				{

				}
				else//inside
				{
					planeBitmask = planeBitmask.UnsetBitAt(id);
				}
				id = ref Unsafe.Add(ref id, 1);
			}
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
		//construct/deconstruct
		//pack/unpack
		//narrow/widen
		public static void Deconstruct(this long p, out int lower, out int upper)
		{
			ref var tmp = ref Unsafe.As<long, TwoInts>(ref p);
			lower = tmp.lower;
			upper = tmp.upper;
		}
	}

	public interface IBroadPhaseFrustumTester
	{
		unsafe void FrustumTest(CollidableReference collidable, FrustumData* frustumData);
	}
	//TODO: with new bepu where workers have void* context try to change this struct so that multithreaded frustum cull test can be fully multithreaded without synchronization (it can be performed on client side if necessary)
	//eg change leaves to array length=treecount
	struct FrustumLeafTester<TFrustumTester> : IFrustumLeafTester where TFrustumTester : struct, IBroadPhaseFrustumTester
	{
		public TFrustumTester LeafTester;
		public Buffer<CollidableReference> leaves;
		public Buffer<CollidableReference> Leaves { set => leaves = value; get => leaves; }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void TestLeaf(int leafIndex, FrustumData* frustumData)
		{
			LeafTester.FrustumTest(leaves[leafIndex], frustumData);
		}
	}
	[StructLayout(LayoutKind.Sequential)]
	public struct FrustumData
	{
		public Plane nearPlane;

		public Plane topPlane;

		public Plane bottomPlane;

		public Plane leftPlane;

		public Plane rightPlane;

		public Plane farPlane;

		public Vector3 conditionNearPlane;

		public Vector3 conditionTopPlane;

		public Vector3 conditionBottomPlane;

		public Vector3 conditionLeftPlane;

		public Vector3 conditionRightPlane;

		public Vector3 conditionFarPlane;

		public int Id;
	}
	public interface IFrustumLeafTester
	{
		Buffer<CollidableReference> Leaves { set; }
		unsafe void TestLeaf(int leafIndex, FrustumData* frustumData);
	}

}
