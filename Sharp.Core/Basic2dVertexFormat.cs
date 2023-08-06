using PluginAbstraction;
using SharpAsset;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp
{
    [Serializable, StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Basic2dVertexFormat : IVertex
    {
        [RegisterAs(ParameterType.FLOAT)]
        public Vector2 vertex_position;
    }
}