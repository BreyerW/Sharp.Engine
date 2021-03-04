using System;
using System.Collections.Generic;
using System.Numerics;
using System.IO;
using Sharp;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sharp.Core;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".fbx", ".dae", ".obj")]
	public class MeshPipeline : Pipeline<Mesh>
	{
		private static readonly VertexAttribute[] supportedAttribs = (VertexAttribute[])Enum.GetValues(typeof(VertexAttribute));
		private static Type vertType;
		private static int vertStride;
		private static int indexStride;
		private static int vec3Stride = Marshal.SizeOf<Vector3>();
		private static int color4Stride = Marshal.SizeOf<Color>();

		static MeshPipeline()
		{
			SetMeshContext<uint, BasicVertexFormat>();
		}

		public static void SetVertexContext<T>() where T : struct, IVertex
		{
			vertType = typeof(T);
			vertStride = Marshal.SizeOf<T>();
		}
		public static void SetIndexContext<T>() where T : struct
		{
			indexStride = Marshal.SizeOf<T>();
		}
		public static void SetMeshContext<TIndex, TVertex>() where TIndex : struct where TVertex : struct, IVertex
		{
			SetIndexContext<TIndex>();
			SetVertexContext<TVertex>();
		}
		public override IAsset Import(string pathToFile)
		{
			if (base.Import(pathToFile) is IAsset asset) return asset;
			var format = Path.GetExtension(pathToFile);
			//if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
			//throw new NotSupportedException (format+" format is not supported");

			var internalMesh = new Mesh
			{
				UsageHint = UsageHint.DynamicDraw,
				FullPath = pathToFile,
				VertType = vertType,
				VBO = -1,
				EBO = -1,
				indexStride = indexStride,
			};
			if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(vertType))
				RegisterAsAttribute.ParseVertexFormat(vertType);

			var vertFormat = RegisterAsAttribute.registeredVertexFormats[vertType];

			byte[] finalIndices = new byte[0];
			byte[] finalVertices = new byte[0];
			int id = 0;
			(Vector3 Min, Vector3 Max) largestBound = default;
			List<int> subMeshesDescription = new();

			foreach (var data in PluginManager.meshLoader.Import(pathToFile))
			{
				if (id is 1)
				{
					subMeshesDescription.Add(finalIndices.Length / indexStride);
					subMeshesDescription.Add(finalVertices.Length);
				}
				Array.Resize(ref finalIndices, finalIndices.Length + data.indices.Length);
				if (id is not 0)
				{
					foreach (var i in ..(data.indices.Length / indexStride))
					{
						ref var addr = ref finalIndices.AsSpan()[finalIndices.Length - data.indices.Length + i * indexStride];
						Unsafe.WriteUnaligned(ref addr, Unsafe.ReadUnaligned<uint>(ref data.indices[i * indexStride]) + (uint)(finalVertices.Length / vertStride));
					}
				}
				else
					Unsafe.CopyBlockUnaligned(ref finalIndices[0], ref data.indices[0], (uint)data.indices.Length);

				largestBound.Min = Vector3.Min(largestBound.Min, data.minExtents);
				largestBound.Max = Vector3.Min(largestBound.Max, data.maxExtents);

				Array.Resize(ref finalVertices, finalVertices.Length + vertStride * data.vertices.Length);

				if (id is not 0)
				{
					subMeshesDescription.Add(finalIndices.Length / indexStride);
					subMeshesDescription.Add(finalVertices.Length);
				}

				if (data.vertices is not null)
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.POSITION];
					CopyBytes(attribProps, data.vertices.Length, finalVertices, MemoryMarshal.AsBytes(data.vertices.AsSpan()), vec3Stride);
				}
				if (data.normals is not null)
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.NORMAL];
					CopyBytes(attribProps, data.vertices.Length, finalVertices, MemoryMarshal.AsBytes(data.normals.AsSpan()), vec3Stride);
				}
				if (data.uv0 is not null)
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.UV];
					CopyBytes(attribProps, data.vertices.Length, finalVertices, MemoryMarshal.AsBytes(data.uv0.AsSpan()), vec3Stride);
				}
				if (data.color0 is not null)
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.COLOR4];
					CopyBytes(attribProps, data.vertices.Length, finalVertices, MemoryMarshal.AsBytes(data.color0.AsSpan()), color4Stride);
				}
				id++;
			}
			internalMesh.Indices = finalIndices;
			internalMesh.verts = finalVertices;
			internalMesh.subMeshesDescriptor = subMeshesDescription.ToArray();
			internalMesh.bounds = new BoundingBox(largestBound.Min, largestBound.Max);
			return this[Register(internalMesh)];
		}

		private void CopyBytes(RegisterAsAttribute format, int count, byte[] vertices, Span<byte> data, int dataStride)
		{
			ref var pointer = ref vertices.AsSpan()[vertices.Length - count * vertStride];
			foreach (var i in ..count)
			{
				ref var addr = ref Unsafe.Add(ref pointer, i * vertStride + format.offset);
				Unsafe.CopyBlockUnaligned(ref addr, ref data[i * dataStride], (uint)format.stride);
			}
		}
		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}
	}
}