using System;
using System.Collections.Generic;
using Assimp;
using Assimp.Configs;
using OpenTK;
using System.IO;
using System.Linq;
using Sharp;

namespace SharpAsset.Pipeline
{
	[SupportedFileFormats(".fbx",".dae",".obj")]
	public class MeshPipeline:Pipeline
	{
		public static readonly MeshPipeline singleton=new MeshPipeline();

		private static BoundingBox bounds;
		private static Func<IVertex> vertex;
		private static Func<Mesh<int>,IAsset> convert;

		public static void SetMeshContext<IndexType,T>() where IndexType : struct,IConvertible where T: struct, IVertex {
			SetIndiceContext<IndexType> ();
			SetVertexContext<T> ();
		}
		public static void SetIndiceContext<IndexType>() where IndexType : struct,IConvertible {
			convert =(mesh)=>(Mesh<IndexType>)mesh;
		}
		public static void SetVertexContext<T>() where T: struct, IVertex {
			vertex = ()=>default(T);
		}

		public override IAsset Import (string pathToFile)
		{
			var format = Path.GetExtension (pathToFile);
			//if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
				//throw new NotSupportedException (format+" format is not supported");
			var vertexType=vertex().GetType();
			var asset = new AssimpContext ();
			var scene=asset.ImportFile (pathToFile,PostProcessPreset.TargetRealTimeMaximumQuality);

			var internalMesh =new Mesh<int>();
			internalMesh.FullPath = pathToFile;
			if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey (vertexType))
				RegisterAsAttribute.ParseVertexFormat (vertexType);
			
			foreach (var mesh in scene.Meshes) {
				//Console.WriteLine ("indices : "+ mesh.GetUnsignedIndices());
				//mesh.MaterialIndex
				//internalMesh.Indices=indexType==typeof(int) ? new int[mesh.VertexCount*3] : new short[mesh.VertexCount*3];
				internalMesh.Indices=mesh.GetIndices();//(ushort[])(object)
				Console.WriteLine(mesh);
				var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				internalMesh.Vertices=new IVertex[mesh.VertexCount];

				for (int i = 0; i < mesh.VertexCount; i++) {
					internalMesh.Vertices [i] = vertex();
					var tmpVec = new Vector3 (mesh.Vertices [i].X, mesh.Vertices [i].Y, mesh.Vertices [i].Z);
					//Console.WriteLine ((maxtmpVec));
					min = Vector3.ComponentMin(min, tmpVec);
					max = Vector3.ComponentMax(max, tmpVec);
					if (mesh.HasVertices && RegisterAsAttribute.registeredVertexFormats [vertexType].ContainsKey (VertexAttribute.POSITION)) {
						if (RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.POSITION].generatedFillers.Count > 1) {
							RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.POSITION].generatedFillers[0] (internalMesh.Vertices [i],mesh.Vertices [i].X);
							RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.POSITION].generatedFillers[1] (internalMesh.Vertices [i],mesh.Vertices [i].Y);
							RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.POSITION].generatedFillers[2] (internalMesh.Vertices [i],mesh.Vertices [i].Z);
						}
					}
					if (mesh.HasVertexColors (0) && RegisterAsAttribute.registeredVertexFormats[vertexType].ContainsKey(VertexAttribute.COLOR)) {
						//baseVertData.Color.r = mesh.VertexColorChannels [0] [i].R;
						//baseVertData.Color.g = mesh.VertexColorChannels [0] [i].G;
						//baseVertData.Color.b = mesh.VertexColorChannels [0] [i].B;
						//baseVertData.Color.a = mesh.VertexColorChannels [0] [i].A;
					}
					if (mesh.HasTextureCoords (0) && RegisterAsAttribute.registeredVertexFormats[vertexType].ContainsKey(VertexAttribute.UV)) {
						if (RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.UV].generatedFillers.Count > 1){}
						else
							RegisterAsAttribute.registeredVertexFormats [vertexType] [VertexAttribute.UV].generatedFillers[0] (internalMesh.Vertices [i],new Vector2(mesh.TextureCoordinateChannels [0] [i].X,1- mesh.TextureCoordinateChannels [0] [i].Y));
					}
				}
				bounds = new BoundingBox (min,max);
				internalMesh.UsageHint = UsageHint.DynamicDraw;
			}
			internalMesh.bounds = bounds;

			return convert?.Invoke(internalMesh) ?? internalMesh;
		}
	//	public Matrix4 CalculateModelMatrix(){

	//	return Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
		//}
		public override void Export (string pathToExport, string format)
		{
			throw new NotImplementedException ();
		}
		private Matrix4 FromMatrix(Matrix4x4 mat)
		{
			Matrix4 m = new Matrix4();
			m.M11 = mat.A1;
			m.M12 = mat.A2;
			m.M13 = mat.A3;
			m.M14 = mat.A4;
			m.M21 = mat.B1;
			m.M22 = mat.B2;
			m.M23 = mat.B3;
			m.M24 = mat.B4;
			m.M31 = mat.C1;
			m.M32 = mat.C2;
			m.M33 = mat.C3;
			m.M34 = mat.C4;
			m.M41 = mat.D1;
			m.M42 = mat.D2;
			m.M43 = mat.D3;
			m.M44 = mat.D4;
			return m;
		}
	}

}

