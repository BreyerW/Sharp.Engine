using System;
using System.Collections.Generic;

//using System.Numerics;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;
using PluginAbstraction;

namespace Sharp
{
	[Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct BasicVertexFormat : IVertex
	{
		[RegisterAs(AttributeType.Float)]
		public Vector3 vertex_position;

		[RegisterAs(AttributeType.Float, "vertex_normal")]
		public Vector3 normal;

		[RegisterAs(AttributeType.Float, "vertex_texcoord")]
		public Vector2 texcoords;
	}
}