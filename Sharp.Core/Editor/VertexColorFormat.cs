using PluginAbstraction;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexColorFormat : IVertex
    {
        //TODO: eliminate naming constraint use GPU API to connect names? and eliminate attributeType same way?
        //or eliminate vertexes completely and rigidly stack vertex attributes in meshpipeline achieving automatic vertex memory optimization (need a way to insert blsnk attributes for procedural scenarios though)
        [RegisterAs(ParameterType.FLOAT, "vertex_position")]
        public Vector3 position;
        [RegisterAs(ParameterType.FLOAT, "vertex_texcoord")]
        public Vector2 texcoords;
        [RegisterAs(ParameterType.FLOAT)]
        public Color vertex_color;
    }
}