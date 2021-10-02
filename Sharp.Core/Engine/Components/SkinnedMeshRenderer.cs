using SharpAsset;
using System;

namespace Sharp
{
    public class SkinnedMeshRenderer : MeshRenderer
    {
        private Skeleton skeleton;

        public SkinnedMeshRenderer(Entity parent) : base(parent)
        {
        }
    }
}