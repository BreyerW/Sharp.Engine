using PluginAbstraction;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Numerics;
//using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BasicVertexFormat : IVertex
    {
        [RegisterAs(ParameterType.FLOAT)]
        public Vector3 vertex_position;

        [RegisterAs(ParameterType.FLOAT, "vertex_normal")]
        public Vector3 normal;

        [RegisterAs(ParameterType.FLOAT, "vertex_texcoord")]
        public Vector2 texcoords;
    }
}