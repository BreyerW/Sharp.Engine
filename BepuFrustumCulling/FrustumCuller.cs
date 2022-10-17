using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Collections;
using BepuUtilities.Memory;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BepuFrustumCulling
{

	public struct DefaultSweepDataGetter : ISweepDataGetter
	{
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

		public ref int GetInsertionIndex(int index)
		{
			if (index is 0) return ref FrustumCuller.FrozenInsertIndex;
			return ref Unsafe.NullRef<int>();
		}
	}
	public interface ISweepDataGetter
	{
		//TODO: for best perf require that largest tree is put as first?
		public ref Tree GetTree(int index);
		public ref Buffer<CollidableReference> GetLeaves(int index);

		//public ref int GetInsertionIndex(int index);
		//public void DeconstructIndexToNodeAndTreeId(int index,out Node node, out int treeId);
	}
	public static class FrustumCuller
	{
		public static int FrozenInsertIndex = -1;
		// internal static int lockfreeGuard = 1;
		internal static BufferPool Pool;
		//representation of 0b1111_1111_1111_1111_1111_1111_1100_0000 on little endian that also works on big endian
		internal const int TraversalStackCapacity = 256;
		internal const uint isInside = 4294967232;

		//TODO: convert to array of dictionaries with length = threadcount
		internal static int totalLeafCount;
		internal static int largestTreeIndex;
		internal static ConcurrentDictionary<int, int> failedPlane = new();
		internal static Buffer<QuickList<int>> leavesToTest;
		internal static int[] startingIndexes = new int[4];
		internal static int startingIndexesLength;
		internal static int currentIndex;
		internal static int currentTreeId;
		internal static int interThreadExchange;
		internal static int interThreadExchangeTreeIndex;
		internal static IThreadDispatcher threadDispatcher;
		internal static int globalRemainingLeavesToVisit;
		internal unsafe static FrustumData* frustumData;
		public static Buffer<CollidableReference> FrozenLeaves;
		public static Tree FrozenTree;
		internal static Tree.RefitAndRefineMultithreadedContext frozenRefineContext;
		internal static int frameIndex;
		internal static int remainingJobCount;
		internal static Action<int> executeRefitAndMarkAction, executeRefineAction;
		//TODO: add [UnscopedAttribute] Once Bepu moves to net 7+ so that this struct can be directly treated as fixed array of items
		[StructLayout(LayoutKind.Sequential)]
		internal record struct FixedArrayOfItems<T> where T : struct
		{
			public T Item0;
			public T Item1;
			public T Item2;
			public T Item3;
			public T Item4;
			public T Item5;
			public T Item6;

			public FixedArrayOfItems(T item1, T item2, T item3, T item4, T item5, T item6)
			{
				Item1 = item1;
				Item2 = item2;
				Item3 = item3;
				Item4 = item4;
				Item5 = item5;
				Item6 = item6;
				Item0 = default;
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
		//private static int lockfreeGuard = 1;
		internal static int treeCount;
		private static T dataGetter;
		private static T DataGetter
		{
			//get { return dataGetter; }
			set
			{
				ref Tree tree = ref value.GetTree(treeCount);
				while (Unsafe.IsNullRef(ref tree) is false)
				{
					FrustumCuller.totalLeafCount += tree.LeafCount;
					tree = ref value.GetTree(++treeCount);
				}
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
		public unsafe void FrustumSweep<TFrustumTester>(in Matrix4x4 matrix, ref TFrustumTester frustumTester, bool columnMajor = false, int id = 0, IThreadDispatcher dispatcher = null) where TFrustumTester : IBroadPhaseFrustumTester
		{
			//TODO: maybe use preprocessor directive instead of bool?
			//#if COLUMNMAJOR
			var frustumData = new FrustumData()
			{
				Id = id
			};
			var planeSpan = new Span<Plane>(&frustumData.nearPlane, 6);
			//var conditionSpan = new Span<Vector<float>>(&frustumData.conditionNearPlane.X, 16 / Vector<int>.Count);
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
			/*conditionSpan[0] = Vector.GreaterThanOrEqual(Unsafe.As<float, Vector<float>>(ref vectors[0].X), Vector<float>.Zero);
			if (Vector<int>.Count is 8)
				conditionSpan[1] = Vector.GreaterThanOrEqual(Unsafe.As<float, Vector<float>>(ref vectors[2].Z), Vector<float>.Zero);

			else if (Vector<int>.Count is 4)
			{
				conditionSpan[1] = Vector.GreaterThanOrEqual(Unsafe.As<float, Vector<float>>(ref vectors[1].Y), Vector<float>.Zero);
				conditionSpan[2] = Vector.GreaterThanOrEqual(Unsafe.As<float, Vector<float>>(ref vectors[2].Z), Vector<float>.Zero);
				conditionSpan[3] = Vector.GreaterThanOrEqual(Unsafe.As<float, Vector<float>>(ref vectors[4].X), Vector<float>.Zero);
			}*/
			int c = 0;
			while (c < 6)
			{
				var normal = planeSpan[c].Normal;
				vectors[c] = new Vector3(normal.X > 0 ? 1f : 0, normal.Y > 0 ? 1f : 0, normal.Z > 0 ? 1f : 0);
				c++;
			}

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



			/*tester.Leaves = activeLeaves;
			ActiveTree.FrustumSweep(&frustumData, ref tester);
			tester.Leaves = staticLeaves;
			frustumData.treeId++;
			StaticTree.FrustumSweep(&frustumData, ref tester);
			frustumData.treeId++;*/
			Unsafe.SkipInit(out FrustumLeafTester<TFrustumTester> tester);
			tester.LeafTester = frustumTester;
			FrustumCuller.frustumData = &frustumData;
			FrustumSweepMultithreaded(0, FrustumCuller.Pool, ref tester, dispatcher);
			//The sweep tester probably relies on mutation to function; copy any mutations back to the original reference.
			//frustumTester = tester.LeafTester;
		}
		/*private static unsafe void FrustumSweep<TLeafTester>(BufferPool pool, ref TLeafTester leafTester, IThreadDispatcher dispatcher) where TLeafTester : IFrustumLeafTester
		{
			ref var tree = ref dataGetter.CurrentTree;
			leafTester.Leaves = dataGetter.CurrentLeaves;
			if (tree.LeafCount == 0)
				return;
			if (tree.LeafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (IntersectsOrInside(ref tree.Nodes[0].A, FrustumCuller.frustumData).IsBitSetAt(6))
				{
					leafTester.TestLeaf(0, FrustumCuller.frustumData);
				}
			}
			else if (dispatcher is null || tree.LeafCount < dispatcher.ThreadCount * 4)
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.

				var stack = stackalloc int[FrustumCuller.TraversalStackCapacity];
				FrustumSweep(0, stack, ref leafTester);
			}
			else
			{
				FrustumSweepMultithreaded(0, pool, ref leafTester, dispatcher);
			}
		}*/
		private static unsafe void TestAABBs(int dataIndex, ref int counter, ref int nodeIndex, ref int leafIndex, ref int fullyContainedStack, ref uint planeBitmask, ref int stackEnd, int* stack)
		{
			ref var tree = ref dataGetter.GetTree(dataIndex);
			if (nodeIndex < 0)
			{
				//This is actually a leaf node.
				leafIndex = Tree.Encode(nodeIndex);
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
						counter--;
					}
					else
						stack[stackEnd++] = node.B.Index;
				}
				else
				{
					var aBitmask = IntersectsOrInside(ref node.A, FrustumCuller.frustumData, planeBitmask);
					var bBitmask = IntersectsOrInside(ref node.B, FrustumCuller.frustumData, planeBitmask);

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
			FrustumCuller.totalLeafCount = 0;
			ref var tree = ref Unsafe.NullRef<Tree>();
			var i = 0;
			while (i < treeCount)
			{
				tree = ref dataGetter.GetTree(i++);
				//FrustumCuller.largestTreeIndex = tree.LeafCount > dataGetter.GetTree(FrustumCuller.largestTreeIndex).LeafCount ? i : FrustumCuller.largestTreeIndex;
				FrustumCuller.totalLeafCount += tree.LeafCount;
			}

			if (dispatcher is null || FrustumCuller.totalLeafCount < dispatcher.ThreadCount * 4)
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.

				var stack = stackalloc int[FrustumCuller.TraversalStackCapacity];
				while (FrustumCuller.currentTreeId < treeCount && tree.LeafCount > 0)
				{
					tree = ref dataGetter.GetTree(FrustumCuller.currentTreeId);
					leafTester.Leaves = dataGetter.GetLeaves(FrustumCuller.currentTreeId);
					//Unsafe.InitBlockUnaligned(stack,0,);
					FrustumSweep(0, stack, ref leafTester);
					FrustumCuller.currentTreeId++;
				}
				return;
			}
			//FrustumCuller.largestTreeIndex = 0;

			FrustumCuller.threadDispatcher = dispatcher;
			FrustumCuller.globalRemainingLeavesToVisit = tree.LeafCount;
			FrustumCuller.currentIndex = -1;
			FrustumCuller.startingIndexesLength = 0;
			FrustumCuller.interThreadExchange = 0;
			//Debug.Assert((nodeIndex >= 0 && nodeIndex < tree.NodeCount) || (Tree.Encode(nodeIndex) >= 0 && Tree.Encode(nodeIndex) < tree.LeafCount));
			//Debug.Assert(tree.LeafCount >= 2, "This implementation assumes all nodes are filled.");

			pool.Take(FrustumCuller.threadDispatcher.ThreadCount, out FrustumCuller.leavesToTest);

			int multithreadingLeafCountThreshold = dataGetter.GetTree(FrustumCuller.largestTreeIndex).LeafCount;

			for (i = 0; i < FrustumCuller.threadDispatcher.ThreadCount; i++)
				FrustumCuller.leavesToTest[i] = new(multithreadingLeafCountThreshold, FrustumCuller.threadDispatcher.GetThreadMemoryPool(i));

			CollectNodesForMultithreadedCulling(ref tree.Nodes, 0, multithreadingLeafCountThreshold);
			FrustumCuller.threadDispatcher.DispatchWorkers(FrustumSweepThreaded);
			for (i = 0; i < treeCount; i++)
			{
				leafTester.Leaves = dataGetter.GetLeaves(i);
				foreach (var leafIndex in FrustumCuller.leavesToTest[i])
					leafTester.TestLeaf(leafIndex, FrustumCuller.frustumData);
			}
			pool.Return(ref FrustumCuller.leavesToTest);
		}
		private static unsafe void FrustumSweep<TLeafTester>(int nodeIndex, int* stack, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			ref var tree = ref dataGetter.GetTree(FrustumCuller.currentTreeId);
			if (tree.LeafCount == 0)
				return;
			if (tree.LeafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if (IntersectsOrInside(ref tree.Nodes[0].A, FrustumCuller.frustumData).IsBitSetAt(6))
				{
					leafTester.TestLeaf(0, FrustumCuller.frustumData);
				}
			}

			Debug.Assert((nodeIndex >= 0 && nodeIndex < tree.NodeCount) || (Tree.Encode(nodeIndex) >= 0 && Tree.Encode(nodeIndex) < tree.LeafCount));
			//Debug.Assert(tree.LeafCount >= 2, "This implementation assumes all nodes are filled.");
			FrustumCuller.globalRemainingLeavesToVisit = tree.LeafCount;
			uint planeBitmask = uint.MaxValue;
			ref int leafIndex = ref Unsafe.AsRef(-1);
			ref int stackEnd = ref Unsafe.AsRef(0);
			ref int fullyContainedStack = ref Unsafe.AsRef(-1);
			while (true)
			{
				TestAABBs(FrustumCuller.currentTreeId, ref FrustumCuller.globalRemainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
				if (leafIndex > -1)
				{
					leafTester.TestLeaf(leafIndex, FrustumCuller.frustumData);
					leafIndex = -1;
				}
				if (FrustumCuller.globalRemainingLeavesToVisit is 0)
					return;
			}
		}
		private static unsafe void FrustumSweepThreaded(int workerIndex)
		{
			var treeId = 0;
			ref var tree = ref dataGetter.GetTree(treeId);
			var stack = stackalloc int[FrustumCuller.TraversalStackCapacity];
			FrustumCuller.threadDispatcher.GetThreadMemoryPool(workerIndex);
			ref var node = ref tree.Nodes[0];
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
					if (leafIndex is not 0)
						Debug.Assert(FrustumCuller.leavesToTest[treeId].Contains(leafIndex) is false, "Duplicates are unacceptable");
					//FrustumCuller.leavesToTest[workerIndex].AddUnsafely(leafIndex);
					var i = Interlocked.Increment(ref FrustumCuller.leavesToTest[treeId].Count);
					FrustumCuller.leavesToTest[treeId][i - 1] = leafIndex;
					leafIndex = -1;
				}
				//TODO remove reliance on globalremainingleavestovisit it should be possible to work with just currentTreeId<treecount or just isnullref
				if (remainingLeavesToVisit is 0)
				{
					nodeIndex = 0;
					leafIndex = -1;
					var newVal = Interlocked.Add(ref FrustumCuller.globalRemainingLeavesToVisit, -takenLeavesCount);
					if (newVal < 1 && Interlocked.Increment(ref FrustumCuller.currentTreeId) < treeCount)
						Interlocked.Add(ref FrustumCuller.globalRemainingLeavesToVisit, dataGetter.GetTree(FrustumCuller.currentTreeId).LeafCount);
					if (treeId != FrustumCuller.currentTreeId)
					{
						treeId = FrustumCuller.currentTreeId;
						tree = ref dataGetter.GetTree(treeId);
						if (Unsafe.IsNullRef(ref tree)) return;
						node = ref tree.Nodes[0];
						nodeIndex = node.A.Index;
					}
					//we no longer have work on this thread. We try to steal some from other threads
					{
						if (FrustumCuller.currentIndex < FrustumCuller.startingIndexesLength)
						{
							var nextExtraStartingIndex = Interlocked.Increment(ref FrustumCuller.currentIndex);
							//due to multithreading it might happen that 2 threads attempt to increment
							//while currentIndex is smaller by 1 than startingIndexesLength
							//resulting in error if we dont check again
							if (nextExtraStartingIndex < FrustumCuller.startingIndexesLength)
								nodeIndex = FrustumCuller.startingIndexes[nextExtraStartingIndex];
						}

						while (nodeIndex is 0 && FrustumCuller.currentTreeId < treeCount)
							nodeIndex = Interlocked.Exchange(ref FrustumCuller.interThreadExchange, 0);
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
							treeId = FrustumCuller.interThreadExchangeTreeIndex;
							tree = ref dataGetter.GetTree(treeId);
							node = ref tree.Nodes[nodeIndex];
							remainingLeavesToVisit = node.A.LeafCount + node.B.LeafCount;
							takenLeavesCount = remainingLeavesToVisit;
						}
						else return;
						leafIndex = -1;
						fullyContainedStack = -1;
						givenUpCount = 0;
					}
				}
				else if (FrustumCuller.globalRemainingLeavesToVisit > 0 && stackEnd > 0)
				{
					//We actively give up one branch when other thread stole it,	
					//to simplify interthread communication
					//provided we have anything to give up
					//We give up branch at the bottom not at the top
					//because bottom branch usually has the most amount of leaves left to check

					var index = stack[0];
					var oldVal = Interlocked.CompareExchange(ref FrustumCuller.interThreadExchange, fullyContainedStack is 0 ? -index : index, 0);

					if (oldVal is 0)
					{
						FrustumCuller.interThreadExchangeTreeIndex = treeId;
						stackEnd--;
						node = ref tree.Nodes[index];
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
				TestAABBs(treeId, ref remainingLeavesToVisit, ref nodeIndex, ref leafIndex, ref fullyContainedStack, ref planeBitmask, ref stackEnd, stack);
			}
		}
		static unsafe void CollectNodesForMultithreadedCulling(ref Buffer<Node> Nodes, int nodeIndex, int leafCountThreshold)
		{
			ref var node = ref Nodes[nodeIndex];

			if (node.A.Index > 0)
				if (node.A.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(ref Nodes, node.A.Index, leafCountThreshold);
				}
				else
				{
					FrustumCuller.startingIndexes[FrustumCuller.startingIndexesLength] = node.A.Index;
					FrustumCuller.startingIndexesLength++;
					if (FrustumCuller.startingIndexesLength == FrustumCuller.startingIndexes.Length)
						Array.Resize(ref FrustumCuller.startingIndexes, FrustumCuller.startingIndexes.Length * 2);
				}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(ref node.A, FrustumCuller.frustumData).IsBitSetAt(6))
					FrustumCuller.leavesToTest[0].AddUnsafely(Tree.Encode(node.A.Index));
				FrustumCuller.globalRemainingLeavesToVisit--;
			}
			if (node.B.Index > 0)
				if (node.B.LeafCount > leafCountThreshold)
				{
					CollectNodesForMultithreadedCulling(ref Nodes, node.B.Index, leafCountThreshold);
				}
				else
				{
					FrustumCuller.startingIndexes[FrustumCuller.startingIndexesLength] = node.B.Index;
					FrustumCuller.startingIndexesLength++;
					if (FrustumCuller.startingIndexesLength == FrustumCuller.startingIndexes.Length)
						Array.Resize(ref FrustumCuller.startingIndexes, FrustumCuller.startingIndexes.Length * 2);
				}
			else
			{
				//we met leaf very early. we might as well test it since this is very rare
				if (IntersectsOrInside(ref node.B, FrustumCuller.frustumData).IsBitSetAt(6))
					//TODO: possible bug with assuming 0 index
					FrustumCuller.leavesToTest[0].AddUnsafely(Tree.Encode(node.B.Index));
				FrustumCuller.globalRemainingLeavesToVisit--;
			}
		}
		//TODO: drop far plane, that gives us 15 floats meaning only 16th float is dead
		//or keep far plane and simply flip near plane after computation
		//TODO: make version with A and B nodes as parameters to fuse their testing instead of min/max vectors and index 
		private unsafe static uint IntersectsOrInside(ref NodeChild node, FrustumData* frustumData, uint planeBitmask = uint.MaxValue)
		{
			var shouldRenumberPlanes = FrustumCuller.failedPlane.TryGetValue(node.Index, out var planeId);

			//Convert AABB to center-extents representation
			//On NET 5+ can skip conversion and instead use Vector.ConditionalSelect & Vector.GreaterThan with Vector128
			//and use p, n-vertex optimization
			/*var eRep = new ExtentsRepresentation()
			{
				center = max + min, // Compute AABB center
				extents = max - min // Compute positive extents
			};*/
			ref var planeAddr = ref frustumData->nearPlane;
			ref var plane = ref frustumData->nearPlane;
			ref var conditionAddr = ref frustumData->conditionNearPlane;
			//far plane test can be eliminated by modyfying lookuptable
			//from 6x6 to 5x5 and deleting every occurance of 5 in table
			//This results in frustum with "infinite" length
			//Note that lookuptable is actually 7x7 but Item0 serves only as padding for pointer shenanigans
			//Also lookuptable could be converted to stackalloc with skipped localsinit making table extremely cheap
			//but algorithm already uses quite large stackalloc for traversing tree so i decided against it
			//especially since table is created once for lifetime of application and is entirely struct-based
			ref var indexesToVisit = ref Unsafe.Add(ref FrustumCuller.lookUpTable.Item1, planeId);
			ref var end = ref indexesToVisit.Item6;
			ref var id = ref indexesToVisit.Item0;
			while (Unsafe.IsAddressLessThan(ref id, ref end))
			{
				id = ref Unsafe.Add(ref id, 1);
				if (!planeBitmask.IsBitSetAt(id))
					continue;
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
					//here, i != planeId test is pointless since planeId is always 0 for this case
					if (!shouldRenumberPlanes && id != 0)
						FrustumCuller.failedPlane.TryAdd(node.Index, id);
					else if (id != 0 && id != planeId)
						FrustumCuller.failedPlane.TryUpdate(node.Index, id, id);
					return planeBitmask;
				}
				if (m < -d)//intersect
				{

				}
				else//inside
				{
					planeBitmask = planeBitmask.UnsetBitAt(id);
				}
			}

			if (shouldRenumberPlanes)
				FrustumCuller.failedPlane.TryRemove(node.Index, out _);

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

	public interface IBroadPhaseFrustumTester
	{
		unsafe void FrustumTest(CollidableReference collidable, FrustumData* frustumData);
	}
	struct FrustumLeafTester<TFrustumTester> : IFrustumLeafTester where TFrustumTester : IBroadPhaseFrustumTester
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
		/*public Plane nearPlane;
		public Plane nearPlaneAbsNormal;

		public Plane topPlane;
		public Plane topPlaneAbsNormal;

		public Plane bottomPlane;
		public Plane bottomPlaneAbsNormal;

		public Plane leftPlane;
		public Plane leftPlaneAbsNormal;

		public Plane rightPlane;
		public Plane rightPlaneAbsNormal;

		public Plane farPlane;
		public Plane farPlaneAbsNormal;*/

		public int Id;
	}
	public interface IFrustumLeafTester
	{
		Buffer<CollidableReference> Leaves { set; }
		unsafe void TestLeaf(int leafIndex, FrustumData* frustumData);
	}

}
