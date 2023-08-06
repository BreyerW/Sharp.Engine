using PluginAbstraction;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct UIVertexFormat : IVertex
    {
        [RegisterAs(ParameterType.FLOAT, "vertex_position")]//TODO: eliminate VertexAttribute and Attribute type and rely on typeof()?
        public Vector3 position;
        [RegisterAs(ParameterType.FLOAT, "vertex_texcoord")]
        public Vector2 texcoords;
    }
}