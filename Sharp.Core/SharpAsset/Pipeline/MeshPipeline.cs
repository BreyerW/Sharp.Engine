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
		private static Func<string, IEnumerable<(string, int, byte[])>> import;

		static MeshPipeline()
		{
			SetMeshContext<uint, BasicVertexFormat>();
			import = PluginManager.ImportAPI<Func<string, IEnumerable<(string, int, byte[])>>>("MeshLoader", "Import");
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
			int vertsCount = 0;
			(Vector3 Min, Vector3 Max) largestBound = default;
			foreach (var (attribName, stride, data) in import(pathToFile))
			{
				if (attribName is "meshCount")
				{
					if (stride > 1)
						internalMesh.subMeshesDescriptor = new int[stride * 2];
				}
				else if (attribName is "indices")
				{
					Array.Resize(ref finalIndices, finalIndices.Length + data.Length);
					if (id is not 0)
					{
						foreach (var i in ..stride)
						{
							ref var addr = ref finalIndices.AsSpan()[finalIndices.Length - data.Length + i * indexStride];
							Unsafe.WriteUnaligned(ref addr, Unsafe.ReadUnaligned<uint>(ref data[i * indexStride]) + (uint)(finalVertices.Length / vertStride));
						}
					}
					else
						Unsafe.CopyBlockUnaligned(ref finalIndices[0], ref data[0], (uint)data.Length);
				}
				else if (attribName is "vertsCount")
				{
					vertsCount = stride;
					Array.Resize(ref finalVertices, finalVertices.Length + vertStride * vertsCount);
				}
				else if (attribName is "vertices")
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.POSITION];
					CopyBytes(attribProps, vertsCount, finalVertices, data, stride);
					if (internalMesh.subMeshesDescriptor is not null)
					{
						internalMesh.subMeshesDescriptor[id * 2] = finalIndices.Length / indexStride;
						internalMesh.subMeshesDescriptor[id * 2 + 1] = finalVertices.Length;
						id++;
					}
				}
				else if (attribName is "normals")
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.NORMAL];
					CopyBytes(attribProps, vertsCount, finalVertices, data, stride);
				}
				else if (attribName[..2] is "uv")
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.UV];
					CopyBytes(attribProps, vertsCount, finalVertices, data, stride);
				}
				else if (attribName[..5] is "color")
				{
					var attribProps = vertFormat.supportedSpecialAttribs[VertexAttribute.NORMAL];
					CopyBytes(attribProps, vertsCount, finalVertices, data, stride);
				}
				else if (attribName is "extents")
				{
					var Min = Unsafe.ReadUnaligned<Vector3>(ref data[0]);
					var Max = Unsafe.ReadUnaligned<Vector3>(ref data[stride]);
					largestBound.Min = Vector3.Min(largestBound.Min, Min);
					largestBound.Max = Vector3.Min(largestBound.Max, Max);
					//if (!Mesh.sharedMeshes.ContainsKey(internalMesh.Name))
					//Mesh.sharedMeshes.Add(internalMesh.Name, vertices.ToArray());
				}
			}
			internalMesh.Indices = finalIndices;
			internalMesh.verts = finalVertices;
			internalMesh.bounds = new Sharp.BoundingBox(largestBound.Min, largestBound.Max);
			return this[Register(internalMesh)];
		}

		private void CopyBytes(RegisterAsAttribute format, int count, byte[] vertices, byte[] data, int dataStride)
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