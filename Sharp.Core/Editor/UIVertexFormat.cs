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
		[RegisterAs(AttributeType.Float, "vertex_position")]//TODO: eliminate VertexAttribute and Attribute type and rely on typeof()?
		public Vector3 position;
		[RegisterAs(AttributeType.Float, "vertex_texcoord")]
		public Vector2 texcoords;
	}
}