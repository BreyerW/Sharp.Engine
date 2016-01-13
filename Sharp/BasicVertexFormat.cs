﻿using System;
using System.Collections.Generic;
//using System.Numerics;
using System.Runtime.InteropServices;
using OpenTK;
using SharpAsset;

namespace Sharp
{
	[Serializable, StructLayout(LayoutKind.Sequential,Pack=1)]
	public struct BasicVertexFormat:IVertex
	{
		[RegisterAs(VertexAttribute.POSITION,VertexType.Float)]
		public float X;

		[RegisterAs(VertexAttribute.POSITION,VertexType.Float)]
		public float Y;

		[RegisterAs(VertexAttribute.POSITION,VertexType.Float)]
		public float Z;

		[RegisterAs(VertexAttribute.UV,VertexType.Float)]
		public Vector2 texcoords;
		//public Color<float> Color;

	/*	public byte[] ConvertToBytes()
		{
			unsafe{
				var target = new byte[Stride];
				fixed(BasicVertexFormat* p=&this) {
					var bytePtr = (byte*)p;
					for (int i = 0; i < Stride; i++) {
						target [i] = *(bytePtr + i);
					}
				}
return target;   
	}
	}*/
	}
}
