﻿using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using SharpAsset;
using SharpAsset.AssetPipeline;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BepuFrustumCulling;
using System.Threading;

namespace Sharp.Physic
{
	public struct testSweepDataGetter : ISweepDataGetter
	{
		public ref Tree GetTree(int index)
		{
			if (index is 0)
				return ref FrustumCuller.FrozenTree;
			else if (index is 1)
				return ref CollisionDetection.simulation.BroadPhase.ActiveTree;
			else
				return ref Unsafe.NullRef<Tree>();
		}

		public ref Buffer<CollidableReference> GetLeaves(int index)
		{
			if (index is 0)
				return ref FrustumCuller.FrozenLeaves;
			else if (index is 1)
				return ref CollisionDetection.simulation.BroadPhase.ActiveLeaves;
			else
				return ref Unsafe.NullRef<Buffer<CollidableReference>>();
		}

		public ref int GetInsertionIndex(int index)
		{
			if (index is 0)
				return ref FrustumCuller.FrozenInsertIndex;
			else if (index is 1) return ref CollisionDetection.ActiveInsertIndex;

			return ref Unsafe.NullRef<int>();
		}

		public int TotalLeafCount => FrustumCuller.FrozenTree.LeafCount + CollisionDetection.simulation.BroadPhase.ActiveTree.LeafCount;
	}
	public static class CollisionDetection
	{
		internal static int ActiveInsertIndex;
		public static BufferPool bufferPool;
		public static Simulation simulation;
		public static FrustumCuller<DefaultSweepDataGetter> frustumCuller;
		internal static Dictionary<Guid, (int index, int handle)> activeMapping = new();
		internal static Dictionary<Guid, (int index, int handle)> staticMapping = new();
		internal static Dictionary<Guid, (int index, int handle, Matrix4x4 mat)> frozenMapping = new();
		internal static Dictionary<int, Guid> frozenIndexMapping = new();
		public static Guid[] inFrustum = new Guid[128];
		public static int inFrustumLength = 0;
		private static int index = 0;
		static CollisionDetection()
		{
			bufferPool = new BufferPool();
			simulation = Simulation.Create(bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, -10, 0)), new SolveDescription(8, 1));
			frustumCuller = FrustumCuller.Create<DefaultSweepDataGetter>(bufferPool);
			//TODO: reverse the situation by defining ref returning func in broadPhase which will pull in custom struct-implemented interfaces ?
		}

		public static int AddFrozenBody(in Guid id, Vector3 pos, Vector3 min, Vector3 max, int existingId = -1)
		{
			var e = id.GetInstanceObject<Entity>();
			var minInWorld = Vector3.Transform(min, e.transform.ModelMatrix); //pos + min;
			var maxInWorld = Vector3.Transform(max, e.transform.ModelMatrix);  //pos + max;
			var box = new BepuUtilities.BoundingBox(minInWorld, maxInWorld);


			if (existingId is -1)
			{
				existingId = index;
				index++;
			}
			var handle = FrustumCuller.AddFrozen(new CollidableReference(new StaticHandle(existingId)), ref box);
			unsafe
			{
				FrustumCuller.FrozenTree.GetBoundsPointers(existingId, out var minp, out var maxp);
				frozenMapping.Add(id, (existingId, handle, Matrix4x4.CreateScale(MathF.Abs(maxp->X - minp->X), MathF.Abs(maxp->Y - minp->Y), MathF.Abs(maxp->Z - minp->Z)) * Matrix4x4.CreateTranslation(min) * e.transform.ModelMatrix));
				frozenIndexMapping.Add(existingId, id);
			}
			if (frozenMapping.Count > inFrustum.Length)
				Array.Resize(ref inFrustum, inFrustum.Length * 2);
			return existingId;
		}
		/*public static void UpdateBody(in Guid id)
		{
			var e = id.GetInstanceObject<Entity>();
			var meshRenderer = e.GetComponent<MeshRenderer>();
			meshRenderer.material.TryGetProperty("mesh", out SharpAsset.Mesh Mesh);
			var dim = Vector3.Abs(Mesh.bounds.Max - Mesh.bounds.Min);                                                                        //maxInWorld = Vector3.Max(minInWorld, maxInWorld);
																																			 //Matrix4x4.CreateFromQuaternion(orientation, out var basis);
			var x = (dim.X / 2f) * e.transform.ModelMatrix.Right();
			var y = (dim.Y / 2f) * e.transform.ModelMatrix.Up();
			var z = (dim.Z / 2f) * e.transform.ModelMatrix.Forward();
			var max = Vector3.Abs(x) + Vector3.Abs(y) + Vector3.Abs(z);
			var min = -max;
			var midpoint = Vector3.Lerp(Mesh.bounds.Min, Mesh.bounds.Max, 0.5f);
			midpoint = Vector3.Transform(midpoint, e.transform.ModelMatrix);
			unsafe
			{
				if (frozenMapping.TryGetValue(id, out var physicId))
				{
					simulation.BroadPhase.UpdateFrozenBounds(physicId.handle, midpoint + min, midpoint + max);
					simulation.BroadPhase.GetFrozenBoundsPointers(physicId.handle, out var minp, out var maxp);

					physicId.mat = Matrix4x4.CreateScale(MathF.Abs(maxp->X - minp->X), MathF.Abs(maxp->Y - minp->Y), MathF.Abs(maxp->Z - minp->Z)) * Matrix4x4.CreateTranslation(midpoint + min);
				
				}
				else if (staticMapping.TryGetValue(id, out var altPhysicId))
				{
					simulation.BroadPhase.UpdateStaticBounds(altPhysicId.handle, midpoint + min, midpoint + max);
				}
				else if (activeMapping.TryGetValue(id, out altPhysicId))
				{
					simulation.BroadPhase.UpdateActiveBounds(altPhysicId.handle, midpoint + min, midpoint + max);
				}
			}
		}*/
		public static void UpdateFrozenBody(in Guid id)
		{
			var e = id.GetInstanceObject<Entity>();
			var meshRenderer = e.GetComponent<MeshRenderer>();
			meshRenderer.material.TryGetProperty(Material.MESHSLOT, out SharpAsset.Mesh Mesh);
			var dim = Vector3.Abs(Mesh.bounds.Max - Mesh.bounds.Min);                                                                        //maxInWorld = Vector3.Max(minInWorld, maxInWorld);
																																			 //Matrix4x4.CreateFromQuaternion(orientation, out var basis);
			var x = (dim.X / 2f) * e.transform.ModelMatrix.Right();
			var y = (dim.Y / 2f) * e.transform.ModelMatrix.Up();
			var z = (dim.Z / 2f) * e.transform.ModelMatrix.Forward();
			var max = Vector3.Abs(x) + Vector3.Abs(y) + Vector3.Abs(z);
			var min = -max;
			var midpoint = (Mesh.bounds.Min + Mesh.bounds.Max) * 0.5f;
			midpoint = Vector3.Transform(midpoint, e.transform.ModelMatrix);
			var physicId = frozenMapping[id];
			FrustumCuller.FrozenTree.UpdateBounds(physicId.handle, midpoint + min, midpoint + max);
			physicId.mat = Matrix4x4.CreateScale(Vector3.Abs(max - min)) * Matrix4x4.CreateTranslation(midpoint + min);
			//TODO: change to CollectionsMarshal.GetValueRef on .NET 6
			frozenMapping[id] = physicId;
		}
		public static void RemoveFrozenBody(in Guid id)
		{
			if (frozenMapping.TryGetValue(id, out var physicId))
			{
				FrustumCuller.FrozenTree.RemoveAt(physicId.handle);
				//simulation.BroadPhase.RemoveFrozenAt(physicId.handle, out _);
				frozenIndexMapping.Remove(physicId.index);
				frozenMapping.Remove(id);
			}
		}
	}


	unsafe struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
	{
		/// <summary>
		/// Performs any required initialization logic after the Simulation instance has been constructed.
		/// </summary>
		/// <param name="simulation">Simulation that owns these callbacks.</param>
		public void Initialize(Simulation simulation)
		{
			//Often, the callbacks type is created before the simulation instance is fully constructed, so the simulation will call this function when it's ready.
			//Any logic which depends on the simulation existing can be put here.
		}

		/// <summary>
		/// Chooses whether to allow contact generation to proceed for two overlapping collidables.
		/// </summary>
		/// <param name="workerIndex">Index of the worker that identified the overlap.</param>
		/// <param name="a">Reference to the first collidable in the pair.</param>
		/// <param name="b">Reference to the second collidable in the pair.</param>
		/// <returns>True if collision detection should proceed, false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
		{
			//Before creating a narrow phase pair, the broad phase asks this callback whether to bother with a given pair of objects.
			//This can be used to implement arbitrary forms of collision filtering. See the RagdollDemo or NewtDemo for examples.
			//Here, we'll make sure at least one of the two bodies is dynamic.
			//The engine won't generate static-static pairs, but it will generate kinematic-kinematic pairs.
			//That's useful if you're trying to make some sort of sensor/trigger object, but since kinematic-kinematic pairs
			//can't generate constraints (both bodies have infinite inertia), simple simulations can just ignore such pairs.
			return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
		}

		/// <summary>
		/// Chooses whether to allow contact generation to proceed for the children of two overlapping collidables in a compound-including pair.
		/// </summary>
		/// <param name="pair">Parent pair of the two child collidables.</param>
		/// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
		/// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
		/// <returns>True if collision detection should proceed, false otherwise.</returns>
		/// <remarks>This is called for each sub-overlap in a collidable pair involving compound collidables. If neither collidable in a pair is compound, this will not be called.
		/// For compound-including pairs, if the earlier call to AllowContactGeneration returns false for owning pair, this will not be called. Note that it is possible
		/// for this function to be called twice for the same subpair if the pair has continuous collision detection enabled; 
		/// the CCD sweep test that runs before the contact generation test also asks before performing child pair tests.</remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
		{
			//This is similar to the top level broad phase callback above. It's called by the narrow phase before generating
			//subpairs between children in parent shapes. 
			//This only gets called in pairs that involve at least one shape type that can contain multiple children, like a Compound.
			return true;
		}

		/// <summary>
		/// Provides a notification that a manifold has been created for a pair. Offers an opportunity to change the manifold's details. 
		/// </summary>
		/// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
		/// <param name="pair">Pair of collidables that the manifold was detected between.</param>
		/// <param name="manifold">Set of contacts detected between the collidables.</param>
		/// <param name="pairMaterial">Material properties of the manifold.</param>
		/// <returns>True if a constraint should be created for the manifold, false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
		{
			//The IContactManifold parameter includes functions for accessing contact data regardless of what the underlying type of the manifold is.
			//If you want to have direct access to the underlying type, you can use the manifold.Convex property and a cast like Unsafe.As<TManifold, ConvexContactManifold or NonconvexContactManifold>(ref manifold).

			//The engine does not define any per-body material properties. Instead, all material lookup and blending operations are handled by the callbacks.
			//For the purposes of this demo, we'll use the same settings for all pairs.
			//(Note that there's no bounciness property! See here for more details: https://github.com/bepu/bepuphysics2/issues/3)
			pairMaterial.FrictionCoefficient = 1f;
			pairMaterial.MaximumRecoveryVelocity = 2f;
			pairMaterial.SpringSettings = new SpringSettings(30, 1);
			//For the purposes of the demo, contact constraints are always generated.
			return true;
		}

		/// <summary>
		/// Provides a notification that a manifold has been created between the children of two collidables in a compound-including pair.
		/// Offers an opportunity to change the manifold's details. 
		/// </summary>
		/// <param name="workerIndex">Index of the worker thread that created this manifold.</param>
		/// <param name="pair">Pair of collidables that the manifold was detected between.</param>
		/// <param name="childIndexA">Index of the child of collidable A in the pair. If collidable A is not compound, then this is always 0.</param>
		/// <param name="childIndexB">Index of the child of collidable B in the pair. If collidable B is not compound, then this is always 0.</param>
		/// <param name="manifold">Set of contacts detected between the collidables.</param>
		/// <returns>True if this manifold should be considered for constraint generation, false otherwise.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
		{
			return true;
		}

		/// <summary>
		/// Releases any resources held by the callbacks. Called by the owning narrow phase when it is being disposed.
		/// </summary>
		public void Dispose()
		{
		}
	}
	public struct BroadPhaseCallback : IBroadPhaseFrustumTester
	{
		public unsafe void FrustumTest(CollidableReference collidable, FrustumData* frustumData)
		{
			CollisionDetection.inFrustum[CollisionDetection.inFrustumLength] = CollisionDetection.frozenIndexMapping[collidable.RawHandleValue];
			CollisionDetection.inFrustumLength++;
			Console.WriteLine("collision");
		}
	}
	//Note that the engine does not require any particular form of gravity- it, like all the contact callbacks, is managed by a callback.
	public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
	{
		/// <summary>
		/// Performs any required initialization logic after the Simulation instance has been constructed.
		/// </summary>
		/// <param name="simulation">Simulation that owns these callbacks.</param>
		public void Initialize(Simulation simulation)
		{
			//In this demo, we don't need to initialize anything.
			//If you had a simulation with per body gravity stored in a CollidableProperty<T> or something similar, having the simulation provided in a callback can be helpful.
		}

		/// <summary>
		/// Gets how the pose integrator should handle angular velocity integration.
		/// </summary>
		public readonly AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

		/// <summary>
		/// Gets whether the integrator should use substepping for unconstrained bodies when using a substepping solver.
		/// If true, unconstrained bodies will be integrated with the same number of substeps as the constrained bodies in the solver.
		/// If false, unconstrained bodies use a single step of length equal to the dt provided to Simulation.Timestep. 
		/// </summary>
		public readonly bool AllowSubstepsForUnconstrainedBodies => false;

		/// <summary>
		/// Gets whether the velocity integration callback should be called for kinematic bodies.
		/// If true, IntegrateVelocity will be called for bundles including kinematic bodies.
		/// If false, kinematic bodies will just continue using whatever velocity they have set.
		/// Most use cases should set this to false.
		/// </summary>
		public readonly bool IntegrateVelocityForKinematics => false;

		public Vector3 Gravity;

		public PoseIntegratorCallbacks(Vector3 gravity) : this()
		{
			Gravity = gravity;
		}

		//Note that velocity integration uses "wide" types. These are array-of-struct-of-arrays types that use SIMD accelerated types underneath.
		//Rather than handling a single body at a time, the callback handles up to Vector<float>.Count bodies simultaneously.
		Vector3Wide gravityWideDt;

		/// <summary>
		/// Callback invoked ahead of dispatches that may call into <see cref="IntegrateVelocity"/>.
		/// It may be called more than once with different values over a frame. For example, when performing bounding box prediction, velocity is integrated with a full frame time step duration.
		/// During substepped solves, integration is split into substepCount steps, each with fullFrameDuration / substepCount duration.
		/// The final integration pass for unconstrained bodies may be either fullFrameDuration or fullFrameDuration / substepCount, depending on the value of AllowSubstepsForUnconstrainedBodies. 
		/// </summary>
		/// <param name="dt">Current integration time step duration.</param>
		/// <remarks>This is typically used for precomputing anything expensive that will be used across velocity integration.</remarks>
		public void PrepareForIntegration(float dt)
		{
			//No reason to recalculate gravity * dt for every body; just cache it ahead of time.
			gravityWideDt = Vector3Wide.Broadcast(Gravity * dt);
		}

		/// <summary>
		/// Callback for a bundle of bodies being integrated.
		/// </summary>
		/// <param name="bodyIndices">Indices of the bodies being integrated in this bundle.</param>
		/// <param name="position">Current body positions.</param>
		/// <param name="orientation">Current body orientations.</param>
		/// <param name="localInertia">Body's current local inertia.</param>
		/// <param name="integrationMask">Mask indicating which lanes are active in the bundle. Active lanes will contain 0xFFFFFFFF, inactive lanes will contain 0.</param>
		/// <param name="workerIndex">Index of the worker thread processing this bundle.</param>
		/// <param name="dt">Durations to integrate the velocity over. Can vary over lanes.</param>
		/// <param name="velocity">Velocity of bodies in the bundle. Any changes to lanes which are not active by the integrationMask will be discarded.</param>
		public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, QuaternionWide orientation, BodyInertiaWide localInertia, Vector<int> integrationMask, int workerIndex, Vector<float> dt, ref BodyVelocityWide velocity)
		{
			//This also is a handy spot to implement things like position dependent gravity or per-body damping.
			//We don't have to check for kinematics; IntegrateVelocityForKinematics returns false in this type, so we'll never see them in this callback.
			//Note that these are SIMD operations and "Wide" types. There are Vector<float>.Count lanes of execution being evaluated simultaneously.
			//The types are laid out in array-of-structures-of-arrays (AOSOA) format. That's because this function is frequently called from vectorized contexts within the solver.
			//Transforming to "array of structures" (AOS) format for the callback and then back to AOSOA would involve a lot of overhead, so instead the callback works on the AOSOA representation directly.
			velocity.Linear += gravityWideDt;
		}

	}

}
