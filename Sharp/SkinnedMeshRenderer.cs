using System;
using SharpAsset;

namespace Sharp
{
	public class SkinnedMeshRenderer<IndexType,VertexFormat>: MeshRenderer<IndexType,VertexFormat> where IndexType : struct,IConvertible where VertexFormat : struct, IVertex
	{
		private Skeleton skeleton;
		public SkinnedMeshRenderer (IAsset meshToRender,Skeleton skele):base(meshToRender)
		{
			skeleton = skele;
		}
	}
}

