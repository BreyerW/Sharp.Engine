using BepuUtilities.Memory;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

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


		internal unsafe void RayCast<TLeafTester>(int nodeIndex, TreeRay* treeRay, RayData* rayData, int* stack, ref TLeafTester leafTester) where TLeafTester : IRayLeafTester
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

		internal unsafe void RayCast<TLeafTester>(TreeRay* treeRay, RayData* rayData, ref TLeafTester leafTester) where TLeafTester : IRayLeafTester
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
		public unsafe void FrustumSweep<TLeafTester>(FrustumData* frustumData, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			if (leafCount == 0)
				return;

			if (leafCount == 1)
			{
				//If the first node isn't filled, we have to use a special case.
				if ((IntersectsOrInside(Nodes[0].A.Min, Nodes[0].A.Max, frustumData, Nodes[0].A.Index) & (1 << 6)) != 0) //
				{
					leafTester.TestLeaf(0, frustumData);
				}
			}
			else
			{
				//TODO: Explicitly tracking depth in the tree during construction/refinement is practically required to guarantee correctness.
				//While it's exceptionally rare that any tree would have more than 256 levels, the worst case of stomping stack memory is not acceptable in the long run.
				var stack = stackalloc int[TraversalStackCapacity];
				FrustumSweep(0, frustumData, stack, ref leafTester);
			}
		}
		//representation of 0b1111_1111_1111_1111_1111_1111_1100_0000 on little endian that also works on big endian
		private const uint isInside = 4294967232;
		private static Dictionary<int, int> failedPlane = new Dictionary<int, int>();

		internal unsafe void FrustumSweep<TLeafTester>(int nodeIndex, FrustumData* frustumData, int* stack, ref TLeafTester leafTester) where TLeafTester : IFrustumLeafTester
		{
			Debug.Assert((nodeIndex >= 0 && nodeIndex < nodeCount) || (Encode(nodeIndex) >= 0 && Encode(nodeIndex) < leafCount));
			Debug.Assert(leafCount >= 2, "This implementation assumes all nodes are filled.");
			uint planeBitmask = uint.MaxValue;
			int stackEnd = 0;
			int fullyContainedStack = -1;
			while (true)
			{
				if (nodeIndex < 0)
				{
					//This is actually a leaf node.
					var leafIndex = Encode(nodeIndex);
					leafTester.TestLeaf(leafIndex, frustumData);
					//Leaves have no children; have to pull from the stack to get a new target.
					if (stackEnd == 0)
						return;
					nodeIndex = stack[--stackEnd];

					//check if we are no longer fully inside frustum. if yes reset fullyContainedStack
					if (stackEnd == fullyContainedStack)
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
						stack[stackEnd++] = node.B.Index;
					}
					else
					{
						var aBitmask = IntersectsOrInside(node.A.Min, node.A.Max, frustumData, node.A.Index, planeBitmask);
						var bBitmask = IntersectsOrInside(node.B.Min, node.B.Max, frustumData, node.B.Index, planeBitmask);

						var aIntersected = (aBitmask & (1 << 6)) != 0;
						var bIntersected = (bBitmask & (1 << 6)) != 0;
						if (aIntersected && !bIntersected)
						{
							nodeIndex = node.A.Index;
							planeBitmask = aBitmask;
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
							stack[stackEnd++] = node.B.Index;
						}
						else if (bIntersected)
						{
							nodeIndex = node.B.Index;
							planeBitmask = bBitmask;
							//check if B child is fully contained in frustum
							//remember we can still intersect at this point and we need to be fully inside
							if (bBitmask == isInside)
								fullyContainedStack = stackEnd;
						}
						else
						{
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
		}
		[StructLayout(LayoutKind.Sequential)]
		struct ExtentsRepresentation
		{
			public Vector3 center;
			private float padding1;
			public Vector3 extents;
			private float padding2;
		}
		public unsafe static uint IntersectsOrInside(in Vector3 min, in Vector3 max, FrustumData* frustumData, int nodeIndex, uint planeBitmask = uint.MaxValue)
		{
			var shouldRenumberPlanes = failedPlane.TryGetValue(nodeIndex, out var planeId);

			var start = 0;
			//far plane test can be eliminated by setting end=5
			//and changing all occurences of start==5 to start==4 instead.
			//This results in frustum with "infinite" length

			var end = 6;

			// Convert AABB to center-extents representation
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
				if (((planeBitmask >> start) & 1) == 0)
				{
					start = shouldRenumberPlanes && start == 5 ? 0 : start + 1;
					continue;
				}
				plane = ref Unsafe.Add(ref planeAddr, start * 2);
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
				}
				else
				{
					m = Vector3.Dot(plane.Normal, eRep.center);
					plane = ref Unsafe.Add(ref plane, 1);//absolute normal
					r = Vector3.Dot(plane.Normal, eRep.extents);
				}
				if (m + r < d)//outside
				{
					planeBitmask &= unchecked((uint)(~(1 << 6)));
					//no need to renumber planes when id is 0
					if (!shouldRenumberPlanes && start != 0 && start != planeId)
						failedPlane.Add(nodeIndex, start);
					else if (start != 0 && start != planeId)
						failedPlane[nodeIndex] = start;
					return planeBitmask;
				}
				if (m - r >= d)//inside
				{
					planeBitmask &= ~(uint)(1 << start);
				}
				/*else//intersect
				{

				}*/
				start = shouldRenumberPlanes && start == 5 ? 0 : start + 1;
			} while (start != end);

			if (shouldRenumberPlanes)
				failedPlane.Remove(nodeIndex);

			return planeBitmask;
		}
		public unsafe void RayCast<TLeafTester>(in Vector3 origin, in Vector3 direction, ref float maximumT, ref TLeafTester leafTester, int id = 0) where TLeafTester : IRayLeafTester
		{
			TreeRay.CreateFrom(origin, direction, maximumT, id, out var rayData, out var treeRay);
			RayCast(&treeRay, &rayData, ref leafTester);
			//The maximumT could have been mutated by the leaf tester. Propagate that change. This is important for when we jump between tree traversals and such.
			maximumT = treeRay.MaximumT;
		}

	}
}
