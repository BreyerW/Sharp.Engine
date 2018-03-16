using System;
using System.Collections.Generic;

//using System.Numerics;
using System.Runtime.InteropServices;
using System.Numerics;
using SharpAsset;

namespace Sharp
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BasicVertexFormat : IVertex
    {
        [RegisterAs(VertexAttribute.POSITION, AttributeType.Float)]
        public Vector3 position;

        [RegisterAs(VertexAttribute.NORMAL, AttributeType.Float)]
        public Vector3 normal;

        /*[RegisterAs(VertexAttribute.POSITION,VertexType.Float)]
		public float Y;

		[RegisterAs(VertexAttribute.POSITION,VertexType.Float)]
		public float Z;*/

        [RegisterAs(VertexAttribute.UV, AttributeType.Float)]
        public Vector2 texcoords;

        //public Color color;
    }
}