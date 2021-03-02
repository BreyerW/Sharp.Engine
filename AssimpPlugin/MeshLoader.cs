using System;
using System.Collections.Generic;
using Assimp;
using System.Threading;
using System.Runtime.InteropServices;

namespace AssimpPlugin
{
	public static class MeshLoader
	{
		private static ThreadLocal<AssimpContext> context = new ThreadLocal<AssimpContext>(() => new AssimpContext());

		public static IEnumerable<MeshData> Import(string pathToFile)
		{
			var scene = context.Value.ImportFile(pathToFile, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipUVs | PostProcessSteps.Triangulate | PostProcessSteps.MakeLeftHanded | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FixInFacingNormals);

			if (!scene.HasMeshes) yield break;

			foreach (var mesh in scene.Meshes)
			{
				var data = new MeshData();

				var indices = mesh.GetUnsignedIndices();//TODO: convert to bytes then switching indices type will be piss easy
				data.indices = MemoryMarshal.AsBytes(indices.AsSpan()).ToArray();
				data.minExtents = mesh.BoundingBox.Min;
				data.maxExtents = mesh.BoundingBox.Max;
				if (mesh.HasVertices)
					data.vertices = mesh.Vertices.ToArray();

				if (mesh.HasNormals)
					data.normals = mesh.Normals.ToArray();
				if (mesh.HasTextureCoords(0))
					data.uv0 = mesh.TextureCoordinateChannels[0].ToArray();
				if (mesh.HasVertexColors(0))
					data.color0 = mesh.VertexColorChannels[0].ToArray();

				yield return data;
			}
			/*if (mesh.HasBones)
				{
					//Get<Skeleton>().scene = scene;
					//foreach (var tree in AssetsView.tree.Values)
					//tree.AddNode(GetPipeline<SkeletonPipeline>().Import(""));
				}*/
		}
	}
	public class MeshData
	{
		public Vector3D minExtents;
		public Vector3D maxExtents;
		public byte[] indices;
		public Vector3D[] vertices;
		public Vector3D[] normals;
		public Vector3D[] uv0;
		public Color4D[] color0;
	}
}
