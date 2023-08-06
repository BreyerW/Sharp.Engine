using PluginAbstraction;
using SharpAsset;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BarycentricVertexFormat : IVertex
    {
        [RegisterAs(ParameterType.FLOAT, "vertex_position")]
        public Vector3 position;
        [RegisterAs(ParameterType.FLOAT, "vertex_texcoord")]
        public Vector2 texcoords;
        [RegisterAs(ParameterType.FLOAT, "vertex_barycentric")]
        public Vector3 barycentric;
    }
}
