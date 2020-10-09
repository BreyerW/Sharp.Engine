using System;
using SharpAsset;

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