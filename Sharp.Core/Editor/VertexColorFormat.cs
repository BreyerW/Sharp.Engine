using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;
using PluginAbstraction;

namespace Sharp.Editor
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct VertexColorFormat : IVertex
	{
		//TODO: eliminate naming constraint use GPU API to connect names? and eliminate attributeType same way?
		//or eliminate vertexes completely and rigidly stack vertex attributes in meshpipeline achieving automatic vertex memory optimization (need a way to insert blsnk attributes for procedural scenarios though)
		[RegisterAs(AttributeType.Float, "vertex_position")]
		public Vector3 position;
		[RegisterAs(AttributeType.Float, "vertex_texcoord")]
		public Vector2 texcoords;
		[RegisterAs(AttributeType.Float)]
		public Color vertex_color;
	}
}