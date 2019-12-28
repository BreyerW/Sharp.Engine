using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;

namespace Sharp.Editor
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct UIVertexFormat : IVertex
	{
		[RegisterAs(VertexAttribute.POSITION, AttributeType.Float)]
		public Vector3 position;
		[RegisterAs(VertexAttribute.UV, AttributeType.Float)]
		public Vector2 texcoords;
	}
}