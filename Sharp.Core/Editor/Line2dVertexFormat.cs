﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;

namespace Sharp.Editor
{
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Line2dVertexFormat : IVertex
	{
		[RegisterAs(AttributeType.Float)]
		public Vector2 vertex_position;
		[RegisterAs(AttributeType.Float)]
		public Vector2 dir;
		[RegisterAs(AttributeType.Float)]
		public float miter;
	}
}