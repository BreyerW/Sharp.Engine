using System;
using System.Collections.Generic;
using Assimp;
using System.Threading;
using System.Runtime.InteropServices;
using PluginAbstraction;

namespace AssimpPlugin
{
	public static class MeshLoader
	{
		private static ThreadLocal<AssimpContext> context = new ThreadLocal<AssimpContext>(() => new AssimpContext());

		public static IEnumerable<(string, int, byte[])> Import(string pathToFile)
		{
			var scene = context.Value.ImportFile(pathToFile, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipUVs | PostProcessSteps.Triangulate | PostProcessSteps.MakeLeftHanded | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FixInFacingNormals);

			if (!scene.HasMeshes) yield break;

			yield return ("meshCount", scene.MeshCount, null);

			foreach (var mesh in scene.Meshes)
			{
				var indices = mesh.GetUnsignedIndices();//TODO: convert to bytes then switching indices type will be piss easy
				yield return ("indices", indices.Length, MemoryMarshal.AsBytes(indices.AsSpan()).ToArray());
				if (mesh.HasBones)
				{
					//Get<Skeleton>().scene = scene;
					//foreach (var tree in AssetsView.tree.Values)
					//tree.AddNode(GetPipeline<SkeletonPipeline>().Import(""));
				}
				if (mesh.HasVertices)
				{
					yield return ("vertsCount", mesh.Vertices.Count, null);
					yield return ("vertices", Marshal.SizeOf<Vector3D>(), MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(mesh.Vertices)).ToArray());
				}
				if (mesh.HasNormals)
					yield return ("normals", Marshal.SizeOf<Vector3D>(), MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(mesh.Normals)).ToArray());
				foreach (var level in ..mesh.TextureCoordinateChannelCount)
				{
					if (mesh.HasTextureCoords(level))
						yield return ($"uv{level}", Marshal.SizeOf<Vector3D>(), MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(mesh.TextureCoordinateChannels[level])).ToArray());
				}
				foreach (var level in ..mesh.VertexColorChannelCount)
				{
					if (mesh.HasVertexColors(level))
						yield return ($"color{level}", Marshal.SizeOf<Color4D>(), MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(mesh.VertexColorChannels[level])).ToArray());
				}
				var extents = new[] { mesh.BoundingBox.Min, mesh.BoundingBox.Max };
				yield return ("extents", Marshal.SizeOf<Vector3D>(), MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref extents[0], 2)).ToArray());
			}
		}
	}
}
