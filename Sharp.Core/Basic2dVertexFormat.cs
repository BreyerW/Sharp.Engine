using System;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;
using SharpSL.BackendRenderers;

namespace Sharp
{
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Basic2dVertexFormat : IVertex
	{
		[RegisterAs(AttributeType.Float)]
		public Vector2 vertex_position;
	}
}