using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PluginAbstraction
{
	public interface IMeshLoaderPlugin : IPlugin
	{
		IEnumerable<MeshData> Import(string pathToFile);
	}
	public class MeshData
	{
		public Vector3 minExtents;
		public Vector3 maxExtents;
		public byte[] indices;
		public Vector3[] vertices;
		public Vector3[] normals;
		public Vector3[] uv0;
		public Vector4[] color0;
	}
}
