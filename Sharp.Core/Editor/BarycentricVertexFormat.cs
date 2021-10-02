using PluginAbstraction;
using SharpAsset;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Sharp.Editor
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BarycentricVertexFormat : IVertex
    {
        [RegisterAs(AttributeType.Float, "vertex_position")]
        public Vector3 position;
        [RegisterAs(AttributeType.Float, "vertex_texcoord")]
        public Vector2 texcoords;
        [RegisterAs(AttributeType.Float, "vertex_barycentric")]
        public Vector3 barycentric;
    }
}
