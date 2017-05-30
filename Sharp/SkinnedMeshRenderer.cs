using System;
using SharpAsset;

namespace Sharp
{
    public class SkinnedMeshRenderer : MeshRenderer
    {
        private Skeleton skeleton;

        public SkinnedMeshRenderer(ref Mesh meshToRender, Material mat, Skeleton skele) : base(ref meshToRender, mat)
        {
            skeleton = skele;
        }
    }
}