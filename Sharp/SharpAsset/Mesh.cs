using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Numerics;
using Sharp;
using SharpAsset.Pipeline;
using SharpAsset;

namespace SharpAsset
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Mesh : IAsset
    {
        public int stride;//talk to vertex format to be able to query exact attribute

        public Type vertType;
        public IndiceType indiceType;

        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }
        public UsageHint UsageHint;
        public byte[] Indices;
        public byte[] verts;
        public BoundingBox bounds;

        internal static Dictionary<string, byte[]> sharedMeshes = new Dictionary<string, byte[]>();

        internal int VBO;
        internal int EBO;

        public Span<byte> SpanToMesh
        {
            get
            {
                return verts is null ? SpanToSharedMesh : verts.AsSpan();
            }
        }

        public Span<byte> SpanToSharedMesh
        {
            get
            {
                return sharedMeshes[Name].AsSpan();
            }
        }

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
            return Name;
        }

        public ref T ReadVertexAtIndex<T>(int index) where T : struct, IVertex
        {
            return ref Unsafe.As<byte, T>(ref SpanToMesh[index * stride]);
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

            var condition = size > stride;
            for (int i = 0; i < SpanToMesh.Length; i++)
            {
                tmpSpan[i] = Unsafe.As<byte, TTo>(ref SpanToMesh.Slice(i * stride, condition ? stride : size)[0]);//check for SpanExiensions.Read/Write
            }
            return tmpSpan;
            //Unsafe.As<byte[], TTo[]>(ref verts);
        }

        public void PlaceIntoScene(Entity context, Vector3 worldPos)//PlaceIntoView(View view,)
        {
            var eObject = new Entity();
            eObject.Position = worldPos;
            var shader = (Shader)Pipeline.Pipeline.GetPipeline<ShaderPipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\TextureOnlyShader.shader");
            //Pipeline.Pipeline.GetPipeline<ShaderPipeline>().GetAsset("TextureOnlyShader");
            var mat = new Material();
            mat.Shader = shader;
            var renderer = new MeshRenderer(ref this, mat);
            eObject.AddComponent(renderer);
            Pipeline.Pipeline.GetPipeline<TexturePipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\duckCM.bmp");
            //zamienic na ref loading pipeliny
            renderer.material.BindProperty("MyTexture", ref Pipeline.Pipeline.GetPipeline<TexturePipeline>().GetAsset("duckCM"));
            if (context != null) //make as child of context?
            {
            }
            eObject.Instatiate();
        }
    }

    public enum UsageHint
    {
        StreamDraw = 35040,
        StreamRead,
        StreamCopy,
        StaticDraw = 35044,
        StaticRead,
        StaticCopy,
        DynamicDraw = 35048,
        DynamicRead,
        DynamicCopy
    }

    public enum IndiceType
    {
        UnsignedByte = 5121,
        UnsignedShort = 5123,
        UnsignedInt = 5125
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