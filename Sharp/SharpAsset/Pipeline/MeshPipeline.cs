using System;
using System.Collections.Generic;
using Assimp;
using System.Threading;
using System.Numerics;
using System.IO;
using Sharp;
using Sharp.Editor.Views;
using System.Runtime.CompilerServices;
using System.Linq;

namespace SharpAsset.Pipeline
{
    [SupportedFiles(".fbx", ".dae", ".obj")]
    public class MeshPipeline : Pipeline<Mesh>
    {
        public ThreadLocal<AssimpContext> asset = new ThreadLocal<AssimpContext>(() => new AssimpContext());
        private static readonly int assimpStride = Unsafe.SizeOf<Vector3D>();
        private static readonly VertexAttribute[] supportedAttribs = (VertexAttribute[])Enum.GetValues(typeof(VertexAttribute));
        private static BoundingBox bounds;
        private static Type vertType;
        private static int size;

        static MeshPipeline()
        {
            SetVertexContext<BasicVertexFormat>();
        }

        public static void SetVertexContext<T>() where T : struct, IVertex
        {
            vertType = typeof(T);
            size = Unsafe.SizeOf<T>();
        }

        public override IAsset Import(string pathToFile)
        {
            var format = Path.GetExtension(pathToFile);
            //if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
            //throw new NotSupportedException (format+" format is not supported");
            var scene = asset.Value.ImportFile(pathToFile, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipUVs | PostProcessSteps.Triangulate | PostProcessSteps.MakeLeftHanded | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FixInFacingNormals);

            if (!scene.HasMeshes) return null;

            var internalMesh = new Mesh();
            internalMesh.FullPath = pathToFile;
            if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(vertType))
                RegisterAsAttribute.ParseVertexFormat(vertType);
            var vertex = Activator.CreateInstance(vertType);//RuntimeHelpers.GetUninitializedObject(type)
            var vertFormat = RegisterAsAttribute.registeredVertexFormats[vertType];
            var finalSupportedAttribs = supportedAttribs.Where((attrib) => vertFormat.ContainsKey(attrib) && scene.Meshes[0].HasAttribute(attrib));
            var meshData = new(VertexAttribute attrib, byte[] data)[finalSupportedAttribs.Count()];
            foreach (var (key, attrib) in finalSupportedAttribs.Indexed())
                meshData[key] = (attrib, null);

            foreach (var mesh in scene.Meshes)
            {
                if (mesh.HasBones)
                {
                    GetPipeline<SkeletonPipeline>().scene = scene;
                    //foreach (var tree in AssetsView.tree.Values)
                    //tree.AddNode(GetPipeline<SkeletonPipeline>().Import(""));
                }
                var indices = mesh.GetUnsignedIndices();
                internalMesh.Indices = indices.AsSpan().AsBytes().ToArray();
                //mesh.has
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                var vertices = new byte[mesh.VertexCount * size].AsSpan();

                foreach (var (key, data) in meshData.Indexed())
                    meshData[key].data = mesh.GetAttribute(data.attrib);

                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    var tmpVec = new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);

                    min = Vector3.Min(min, tmpVec);
                    max = Vector3.Max(max, tmpVec);

                    foreach (var (key, data) in meshData.Indexed())
                        CopyBytes(vertFormat[data.attrib], i, ref meshData[key].data, ref vertices);
                }

                internalMesh.UsageHint = UsageHint.DynamicDraw;
                internalMesh.stride = size;
                internalMesh.vertType = vertType;
                if (indices[0].GetType() == typeof(ushort))
                    internalMesh.indiceType = IndiceType.UnsignedShort;
                else if (indices[0].GetType() == typeof(uint))
                    internalMesh.indiceType = IndiceType.UnsignedInt;
                if (!Mesh.sharedMeshes.ContainsKey(internalMesh.Name))
                    Mesh.sharedMeshes.Add(internalMesh.Name, vertices.ToArray());

                bounds = new BoundingBox(min, max);
            }
            internalMesh.bounds = bounds;
            return internalMesh;
        }

        private void CopyBytes(RegisterAsAttribute format, int index, ref byte[] attribBytes, ref Span<byte> vertBytes)
        {
            var offset = index * size + format.offset;
            var slice = vertBytes.Slice(offset, format.stride);
            for (var i = 0; i < format.stride; i++)
                slice[i] = attribBytes[index * assimpStride + i];
        }

        //	public Matrix4 CalculateModelMatrix(){
        //	return Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        //}
        public override void Export(string pathToExport, string format)
        {
            throw new NotImplementedException();
        }

        public override ref Mesh GetAsset(int index)
        {
            throw new NotImplementedException();
        }
    }

    internal static class AssmipMeshExtension
    {
        public static bool HasAttribute(this Assimp.Mesh mesh, VertexAttribute vertAttrib, int level = 0)
        {
            switch (vertAttrib)
            {
                case VertexAttribute.POSITION: return true;
                case VertexAttribute.UV: return mesh.HasTextureCoords(level);
                case VertexAttribute.NORMAL: return mesh.HasNormals;
                case VertexAttribute.COLOR: return mesh.HasVertexColors(level);
                default: return false;
            }
        }

        public static byte[] GetAttribute(this Assimp.Mesh mesh, VertexAttribute vertAttrib, int level = 0)
        {
            switch (vertAttrib)
            {
                case VertexAttribute.POSITION: return mesh.Vertices.ToArray().AsSpan().AsBytes().ToArray();
                case VertexAttribute.UV: return mesh.TextureCoordinateChannels[level].ToArray().AsSpan().AsBytes().ToArray();
                case VertexAttribute.NORMAL: return mesh.Normals.ToArray().AsSpan().AsBytes().ToArray();
                case VertexAttribute.COLOR: return mesh.VertexColorChannels[level].ToArray().AsSpan().AsBytes().ToArray();
                default: throw new NotSupportedException(vertAttrib + " attribute not supported");
            }
        }
    }

    internal class Pinnable
    {
        public byte Pin;
    }

    /*[SupportedFiles(".fbx", ".dae", ".obj")]
    public class MeshPipeline : Pipeline<Mesh>
    {
        public ThreadLocal<AssimpContext> asset = new ThreadLocal<AssimpContext>(() => new AssimpContext());

        private static BoundingBox bounds;
        private static Type vertType;
        private static Func<IVertex[], byte[]> convertVerts;
        private static int size;

        static MeshPipeline()
        {
            SetVertexContext<BasicVertexFormat>();
        }

        public static void SetVertexContext<T>() where T : struct, IVertex
        {
            vertType = typeof(T);
            size = Unsafe.SizeOf<T>();
        }

        public override IAsset Import(string pathToFile)
        {
            var format = Path.GetExtension(pathToFile);
            //if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
            //throw new NotSupportedException (format+" format is not supported");

            var scene = asset.Value.ImportFile(pathToFile, PostProcessPreset.TargetRealTimeMaximumQuality | PostProcessSteps.FlipUVs | PostProcessSteps.Triangulate | PostProcessSteps.MakeLeftHanded | PostProcessSteps.GenerateSmoothNormals | PostProcessSteps.FixInFacingNormals);
            var internalMesh = new Mesh();
            internalMesh.FullPath = pathToFile;
            if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey(vertType))
                RegisterAsAttribute.ParseVertexFormat(vertType);
            var vertex = Activator.CreateInstance(vertType);//RuntimeHelpers.GetUninitializedObject(type)
            foreach (var mesh in scene.Meshes)
            {
                if (mesh.HasBones)
                {
                    GetPipeline<SkeletonPipeline>().scene = scene;
                    foreach (var tree in AssetsView.tree.Values)
                        tree.AddNode(GetPipeline<SkeletonPipeline>().Import(""));
                }
                var indices = mesh.GetUnsignedIndices();
                internalMesh.Indices = new Span<uint>(indices).AsBytes().ToArray();//(ushort[])(object)
                //mesh.has
                var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                var vertices = new IVertex[mesh.VertexCount];
                var vertsCopy = Array.CreateInstance(vertType, mesh.VertexCount);

                var watch = System.Diagnostics.Stopwatch.StartNew();
                for (int i = 0; i < mesh.VertexCount; i++)
                {
                    vertices[i] = RuntimeHelpers.GetObjectValue(vertex) as IVertex;

                    var tmpVec = new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z);

                    min = Vector3.ComponentMin(min, tmpVec);
                    max = Vector3.ComponentMax(max, tmpVec);
                    if (mesh.HasVertices)
                    {
                        FillVertexAttrib(vertType, VertexAttribute.POSITION, ref vertices[i], new Vector3(mesh.Vertices[i].X, mesh.Vertices[i].Y, mesh.Vertices[i].Z)); //redo with ref-return?
                    }
                    if (mesh.HasVertexColors(0) && RegisterAsAttribute.registeredVertexFormats[vertType].ContainsKey(VertexAttribute.COLOR))
                    {
                        //baseVertData.Color.r = mesh.VertexColorChannels [0] [i].R;
                        //baseVertData.Color.g = mesh.VertexColorChannels [0] [i].G;
                        //baseVertData.Color.b = mesh.VertexColorChannels [0] [i].B;
                        //baseVertData.Color.a = mesh.VertexColorChannels [0] [i].A;
                    }
                    if (mesh.HasTextureCoords(0))
                    {
                        FillVertexAttrib(vertType, VertexAttribute.UV, ref vertices[i], new Vector2(mesh.TextureCoordinateChannels[0][i].X, mesh.TextureCoordinateChannels[0][i].Y));//was 1-texcoord.y
                    }
                    if (mesh.HasNormals)
                    {
                        FillVertexAttrib(vertType, VertexAttribute.NORMAL, ref vertices[i], new Vector3(mesh.Normals[i].X, mesh.Normals[i].Y, mesh.Normals[i].Z));
                    }
                    //vertsCopy.SetValue(vertices[i], i);
                }
                Array.Copy(vertices, vertsCopy, vertsCopy.Length);
                watch.Stop();
                Console.WriteLine("cast: " + watch.ElapsedTicks);
                internalMesh.UsageHint = UsageHint.DynamicDraw;
                internalMesh.stride = size;
                internalMesh.vertType = vertType;
                if (indices[0].GetType() == typeof(ushort))
                    internalMesh.indiceType = IndiceType.UnsignedShort;
                else if (indices[0].GetType() == typeof(uint))
                    internalMesh.indiceType = IndiceType.UnsignedInt;
                var ptr = GCHandle.Alloc(vertsCopy, GCHandleType.Pinned);
                var bytes = Span<byte>.DangerousCreate(vertsCopy, ref Unsafe.As<byte[]>(vertsCopy)[0], vertsCopy.Length * size).ToArray();
                ptr.Free();
                if (!Mesh.sharedMeshes.ContainsKey(internalMesh.Name))
                    Mesh.sharedMeshes.Add(internalMesh.Name, bytes);

                bounds = new BoundingBox(min, max);
            }
            internalMesh.bounds = bounds;

            return internalMesh;
        }

        private void FillVertexAttrib(Type vertType, VertexAttribute vertAttrib, ref IVertex vert, Vector3 param)
        {
            if (!RegisterAsAttribute.registeredVertexFormats[vertType].ContainsKey(vertAttrib))
                return;
            if (RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers.Count > 1)
            {
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[0](vert, param.X);
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[1](vert, param.Y);
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[2](vert, param.Z);
            }
            else
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[0](vert, param);
        }

        private void FillVertexAttrib(Type vertType, VertexAttribute vertAttrib, ref IVertex vert, Vector2 param)
        {
            if (!RegisterAsAttribute.registeredVertexFormats[vertType].ContainsKey(vertAttrib))
                return;
            if (RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers.Count > 1)
            {
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[0](vert, param.X);
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[1](vert, param.Y);
            }
            else
                RegisterAsAttribute.registeredVertexFormats[vertType][vertAttrib].generatedFillers[0](vert, param);
        }

        //	public Matrix4 CalculateModelMatrix(){
        //	return Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
        //}
        public override void Export(string pathToExport, string format)
        {
            throw new NotImplementedException();
        }

        public override ref Mesh GetAsset(int index)
        {
            throw new NotImplementedException();
        }
    }

     */
}