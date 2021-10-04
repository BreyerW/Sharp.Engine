using PluginAbstraction;
using Sharp;
using SharpAsset.AssetPipeline;
using SharpSL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpAsset
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Mesh : IAsset
    {
        public int vertStride;//talk to vertex format to be able to query exact attribute
        public int indexStride;
        public Type VertType //TODO: make LoadVertices<T> where T: struct, IVertex on Mesh and here set up VertType same for indices LoadIndices(); 
        {
            set
            {
                if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(value))
                    RegisterAsAttribute.ParseVertexFormat(value);
                vertStride = Marshal.SizeOf(value);
                vertsType = value;
            }
            get => vertsType;
        }
        private Type vertsType;
        public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }

        public string FullPath { get; set; }
        public UsageHint UsageHint; //TODO: make enum flag with dynamic/static size & attributes static size but dynamic attrib mean unchanging indices but changing vertexes themselves

        internal byte[] Indices;
        internal byte[] verts;
        internal (int start,int end)[] subMeshesDescriptor;
        public BoundingBox bounds;

        internal static Dictionary<string, byte[]> sharedMeshes = new Dictionary<string, byte[]>();

        internal int VBO;
        internal int EBO;

        public Span<byte> SpanToMesh
        {
            get
            {
                return /*verts is null ? SpanToSharedMesh :*/ verts.AsSpan();
            }
        }

        /*public Span<byte> SpanToSharedMesh
		{
			get
			{
				return sharedMeshes[Name].AsSpan();
			}
		}*/

        public Span<byte> SpanToIndices
        {
            get
            {
                return Indices.AsSpan();
            }
        }

        public bool isMeshShared
        {
            get { return verts is null; }
            set
            {
                verts = value ? null : SpanToMesh.ToArray();
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }

        public ref T ReadVertexAtIndex<T>(int index) where T : struct, IVertex
        {
            return ref Unsafe.As<byte, T>(ref verts[index * vertStride]);
        }

        //public TExpected ReadVertexAttributeAtIndex<TExpected>(VertexAttribute attrib,int layer, int index) where TExpected : struct {
        //consult vertex format in order to read only attributes
        //}
        public Span<TTo> SnapshotConvertToFormat<TTo>() where TTo : struct, IVertex //PermamentConvertToFormat
        {
            var size = Unsafe.SizeOf<TTo>();
            //if (size == stride)
            //    return
            var tmpSpan = new Span<TTo>(new TTo[SpanToMesh.Length]);//danger! copy instead of reference. Change it so that unamaged memory is resized and this new span point to resized memory?

            var condition = size > vertStride;
            for (int i = 0; i < SpanToMesh.Length; i++)
            {
                tmpSpan[i] = Unsafe.As<byte, TTo>(ref SpanToMesh.Slice(i * vertStride, condition ? vertStride : size)[0]);//check for SpanExiensions.Read/Write use Unsafe.Write to avoid unaligned issue?
            }
            return tmpSpan;
            //Unsafe.As<byte[], TTo[]>(ref verts);
        }
        public void LoadVertices(byte[] vertices)
        {
            verts = vertices;
        }
        public void LoadIndices(byte[] indices)
        {
            Indices = indices;
        }
        public void LoadVertices<T>(Span<T> vertices) where T : struct, IVertex
        {
            VertType = typeof(T);
            verts = MemoryMarshal.AsBytes(vertices).ToArray();
        }
        public void LoadIndices<T>(Span<T> indices) where T : struct
        {
            indexStride = Marshal.SizeOf<T>();
            Indices = MemoryMarshal.AsBytes(indices).ToArray();
        }
        public void AddSubMesh(ref Mesh mesh, bool merge = false)
        {
            if (vertStride != mesh.vertStride || indexStride != mesh.indexStride)
                throw new ArgumentException("mesh has incompatible vertex or index stride");
            var oldVertsLength = verts.Length;
            var oldIndicesLength = Indices.Length;
            if (subMeshesDescriptor is not null && merge is false)
                Array.Resize(ref subMeshesDescriptor, subMeshesDescriptor.Length + 1);
            else if (merge is false)
            {
                subMeshesDescriptor = new (int,int)[2];
                subMeshesDescriptor[0] = (Indices.Length / indexStride, verts.Length / vertStride);
                //subMeshesDescriptor[0].end = verts.Length / vertStride;
            }
            if (merge is false)
            {
                subMeshesDescriptor[^1] = ((Indices.Length + mesh.Indices.Length) / indexStride,(verts.Length + mesh.verts.Length) / mesh.vertStride);
                //subMeshesDescriptor[^1]= ;
            }
            Array.Resize(ref verts, verts.Length + mesh.verts.Length);
            var slice = verts.AsSpan()[oldVertsLength..];
            mesh.verts.CopyTo(slice);
            Array.Resize(ref Indices, Indices.Length + mesh.Indices.Length);

            var indicesSlice = SpanToIndices[oldIndicesLength..];
            if (indexStride is 2)
                foreach (var i in ..(mesh.Indices.Length / indexStride))
                {
                    ref var index = ref Unsafe.As<byte, ushort>(ref mesh.Indices[i * indexStride]);
                    index += (ushort)(oldVertsLength / mesh.vertStride);
                    MemoryMarshal.Write(indicesSlice[(i * indexStride)..], ref index);
                }
            else
                foreach (var i in ..(mesh.Indices.Length / indexStride))
                {
                    ref var index = ref Unsafe.As<byte, uint>(ref mesh.Indices[i * indexStride]);
                    index += (uint)(oldVertsLength / mesh.vertStride);
                    MemoryMarshal.Write(indicesSlice[(i * indexStride)..], ref index);
                }
        }

    }

    /*public readonly struct SubMeshDescriptor
	{
		public readonly int lastVertex;
	public readonly int lastIndice;
	}*/
    public enum IndiceType
    {
        UnsignedShort,
        UnsignedInt
    }

    public class SafePointer : SafeHandle
    {
        public SafePointer(IntPtr invalidHandleValue) : base(invalidHandleValue, true)
        {
            SetHandle(invalidHandleValue);
        }

        public override bool IsInvalid
        {
            [System.Security.SecurityCritical]
            get { return handle == new IntPtr(-1); }
        }

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}