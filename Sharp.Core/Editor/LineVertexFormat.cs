﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;

namespace Sharp.Editor
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct LineVertexFormat : IVertex
	{
		[RegisterAs(AttributeType.Float)]
		public Vector3 vertex_position;
		[RegisterAs(AttributeType.Float)]
		public Vector3 prev_position;
		[RegisterAs(AttributeType.Float)]
		public Vector3 next_position;
		[RegisterAs(AttributeType.Float)]
		public float dir;
	}
}