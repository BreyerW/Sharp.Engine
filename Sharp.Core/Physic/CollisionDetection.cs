using BepuPhysics;
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

namespace Sharp.Physic
{
	public static class CollisionDetection
	{
		public static BufferPool bufferPool;
		public static Simulation simulation;
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
			simulation = Simulation.Create(bufferPool, new NarrowPhaseCallbacks(), new PoseIntegratorCallbacks(new Vector3(0, -10, 0)), new PositionLastTimestepper());
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
			var handle = simulation.BroadPhase.AddFrozen(new CollidableReference(new StaticHandle(existingId)), ref box);
			unsafe
			{
				simulation.BroadPhase.GetFrozenBoundsPointers(existingId, out var minp, out var maxp);
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
			var midpoint = Vector3.Lerp(Mesh.bounds.Min, Mesh.bounds.Max, 0.5f);
			midpoint = Vector3.Transform(midpoint, e.transform.ModelMatrix);
			var physicId = frozenMapping[id];
			simulation.BroadPhase.UpdateFrozenBounds(physicId.handle, midpoint + min, midpoint + max);
			physicId.mat = Matrix4x4.CreateScale(MathF.Abs(max.X - min.X), MathF.Abs(max.Y - min.Y), MathF.Abs(max.Z - min.Z)) * Matrix4x4.CreateTranslation(midpoint + min);
			//TODO: change to CollectionsMarshal.GetValueRef on .NET 6
			frozenMapping[id] = physicId;
		}
		public static void RemoveFrozenBody(in Guid id)
		{
			if (frozenMapping.TryGetValue(id, out var physicId))
			{
				simulation.BroadPhase.RemoveFrozenAt(physicId.handle, out _);
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
		public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b)
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
		public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : struct, IContactManifold<TManifold>
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
			//Console.WriteLine("collision");
		}
	}
	//Note that the engine does not require any particular form of gravity- it, like all the contact callbacks, is managed by a callback.
	public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
	{
		public Vector3 Gravity;
		Vector3 gravityDt;

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
		public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving; //Don't care about fidelity in this demo!

		public PoseIntegratorCallbacks(Vector3 gravity) : this()
		{
			Gravity = gravity;
		}

		/// <summary>
		/// Called prior to integrating the simulation's active bodies. When used with a substepping timestepper, this could be called multiple times per frame with different time step values.
		/// </summary>
		/// <param name="dt">Current time step duration.</param>
		public void PrepareForIntegration(float dt)
		{
			gravityDt = Gravity * dt;
		}

		/// <summary>
		/// Callback called for each active body within the simulation during body integration.
		/// </summary>
		/// <param name="bodyIndex">Index of the body being visited.</param>
		/// <param name="pose">Body's current pose.</param>
		/// <param name="localInertia">Body's current local inertia.</param>
		/// <param name="workerIndex">Index of the worker thread processing this body.</param>
		/// <param name="velocity">Reference to the body's current velocity to integrate.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void IntegrateVelocity(int bodyIndex, in RigidPose pose, in BodyInertia localInertia, int workerIndex, ref BodyVelocity velocity)
		{
			//Note that we avoid accelerating kinematics. Kinematics are any body with an inverse mass of zero (so a mass of ~infinity). No force can move them.
			if (localInertia.InverseMass > 0)
			{
				velocity.Linear = velocity.Linear + gravityDt;
			}
		}

	}
}
