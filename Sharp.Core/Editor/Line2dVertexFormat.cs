using PluginAbstraction;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Line2dVertexFormat : IVertex
    {
        [RegisterAs(ParameterType.FLOAT)]
        public Vector2 vertex_position;
        [RegisterAs(ParameterType.FLOAT)]
        public Vector2 dir;
        [RegisterAs(ParameterType.FLOAT)]
        public float miter;
    }
}