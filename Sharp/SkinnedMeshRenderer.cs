using System;
using SharpAsset;

namespace Sharp
{
    public class SkinnedMeshRenderer<IndexType> : MeshRenderer<IndexType> where IndexType : struct, IConvertible //where VertexFormat : struct, IVertex
    {
        private Skeleton skeleton;
        public SkinnedMeshRenderer(IAsset meshToRender, Material mat, Skeleton skele) : base(meshToRender, mat)
        {
            skeleton = skele;
        }
    }
}

