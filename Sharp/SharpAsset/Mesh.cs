using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using OpenTK;
using Sharp;
using Antmicro.Migrant;
using SharpAsset.Pipeline;
using SharpAsset;

namespace SharpAsset
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Mesh<IndexType> : IAsset where IndexType : struct, IConvertible
    {
        public int stride;
        public int length;

        [Transient]
        public Type vertType;

        public bool alwaysLocal;
        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }
        public UsageHint UsageHint;
        public IndexType[] Indices;
        public BoundingBox bounds;

        [Transient]
        internal static Dictionary<string, SafePointer> sharedMeshes = new Dictionary<string, SafePointer>();

        internal static IndiceType indiceType = IndiceType.UnsignedByte;

        internal int VBO;
        internal int EBO;

        [Transient]
        internal SafePointer ptrToVerts;

        public SafePointer PtrToLocalMesh
        {
            get
            {
                if (isMeshShared)
                {
                    IntPtr ptr = Marshal.AllocHGlobal(stride * length);
                    var ptrMesh = PtrToSharedMesh.DangerousGetHandle();
                    byte meshByte;
                    for (int i = 0; i < stride * length; i++)
                    {
                        meshByte = Marshal.ReadByte(ptrMesh, i);
                        Marshal.WriteByte(ptr, i, meshByte);
                    }
                    ptrToVerts = new SafePointer(ptr);
                }
                return ptrToVerts;
            }
        }

        public SafePointer PtrToSharedMesh
        {
            get
            {
                return Mesh<int>.sharedMeshes[Name];
            }
        }

        public bool isMeshShared
        {
            get { return ptrToVerts == null; }
        }

        static Mesh()
        {
            if (typeof(IndexType) == typeof(ushort))
                indiceType = IndiceType.UnsignedShort;
            else if (typeof(IndexType) == typeof(uint))
                indiceType = IndiceType.UnsignedInt;
        }

        public override string ToString()
        {
            return Name;
        }

        public void PlaceIntoScene(Entity context, Vector3 worldPos)//PlaceIntoView(View view,)
        {
            var eObject = new Entity();
            eObject.Position = worldPos;
            var shader = (Shader)Pipeline.Pipeline.GetPipeline<ShaderPipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\TextureOnlyShader.shader");
            //Pipeline.Pipeline.GetPipeline<ShaderPipeline>().GetAsset("TextureOnlyShader");
            var mat = new Material();
            mat.Shader = shader;
            var renderer = new MeshRenderer<IndexType>(this, mat);
            eObject.AddComponent(renderer);
            Pipeline.Pipeline.GetPipeline<TexturePipeline>().Import(@"B:\Sharp.Engine3\Sharp\bin\Debug\Content\duckCM.bmp");
            //zamienic na ref loading pipeliny
            renderer.material.BindProperty("MyTexture", () => { return ref Pipeline.Pipeline.GetPipeline<TexturePipeline>().GetAsset("duckCM"); });
            if (context != null) //make as child of context?
            {
            }
            eObject.Instatiate();
        }

        public static explicit operator Mesh<IndexType>(Mesh<int> mesh)
        {
            var shortMesh = new Mesh<IndexType>();
            shortMesh.UsageHint = mesh.UsageHint;
            shortMesh.bounds = mesh.bounds;
            shortMesh.FullPath = mesh.FullPath;
            shortMesh.stride = mesh.stride;
            shortMesh.length = mesh.length;
            shortMesh.vertType = mesh.vertType;
            shortMesh.alwaysLocal = mesh.alwaysLocal;
            //shortMesh.ptrToVerts = mesh.ptrToVerts;
            if (mesh.Indices != null)
            {
                shortMesh.Indices = new IndexType[mesh.Indices.Length];

                //Marshal.copy
                for (var indice = 0; indice < mesh.Indices.Length; indice++)
                    shortMesh.Indices[indice] = (IndexType)Convert.ChangeType(mesh.Indices[indice], typeof(IndexType));
            }
            return shortMesh;
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