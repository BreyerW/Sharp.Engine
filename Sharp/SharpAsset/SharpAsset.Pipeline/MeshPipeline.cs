using System;
using System.Collections.Generic;
using Assimp;
using Assimp.Configs;
using OpenTK;
using System.IO;
using System.Linq;
using Sharp;
using Sharp.Editor.Views;

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

			var scene=asset.ImportFile (pathToFile,PostProcessPreset.TargetRealTimeMaximumQuality|PostProcessSteps.FlipUVs|PostProcessSteps.Triangulate|PostProcessSteps.MakeLeftHanded|PostProcessSteps.GenerateSmoothNormals|PostProcessSteps.FixInFacingNormals);

			var internalMesh =new Mesh<int>();
			internalMesh.FullPath = pathToFile;
			if (!RegisterAsAttribute.registeredVertexFormats.ContainsKey (vertexType))
				RegisterAsAttribute.ParseVertexFormat (vertexType);
			foreach (var mesh in scene.Meshes) {
				//Console.WriteLine ("indices : "+ mesh.GetUnsignedIndices());
				//mesh.MaterialIndex
				//internalMesh.Indices=indexType==typeof(int) ? new int[mesh.VertexCount*3] : new short[mesh.VertexCount*3];
				if (mesh.HasBones) {
					
					SkeletonPipeline.singleton.scene = scene;
					AssetsView.tree.AddNode(SkeletonPipeline.singleton.Import (""));
				}
				internalMesh.Indices=mesh.GetIndices();//(ushort[])(object)
				//mesh.has
				var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
				internalMesh.Vertices=new IVertex[mesh.VertexCount];

				for (int i = 0; i < mesh.VertexCount; i++) {
					internalMesh.Vertices [i] = vertex();
					var tmpVec = new Vector3 (mesh.Vertices [i].X, mesh.Vertices [i].Y, mesh.Vertices [i].Z);
					//Console.WriteLine ((maxtmpVec));
					min = Vector3.ComponentMin(min, tmpVec);
					max = Vector3.ComponentMax(max, tmpVec);
					if (mesh.HasVertices) {
						FillVertexAttrib (vertexType,VertexAttribute.POSITION,ref internalMesh.Vertices[i],new Vector3(mesh.Vertices[i].X,mesh.Vertices[i].Y,mesh.Vertices[i].Z));
					}
					if (mesh.HasVertexColors (0) && RegisterAsAttribute.registeredVertexFormats[vertexType].ContainsKey(VertexAttribute.COLOR)) {
						//baseVertData.Color.r = mesh.VertexColorChannels [0] [i].R;
						//baseVertData.Color.g = mesh.VertexColorChannels [0] [i].G;
						//baseVertData.Color.b = mesh.VertexColorChannels [0] [i].B;
						//baseVertData.Color.a = mesh.VertexColorChannels [0] [i].A;
					}
					if (mesh.HasTextureCoords (0)) {
						FillVertexAttrib (vertexType,VertexAttribute.UV,ref internalMesh.Vertices[i],new Vector2(mesh.TextureCoordinateChannels [0] [i].X, mesh.TextureCoordinateChannels [0] [i].Y));//was 1-texcoord.y
					}
					if (mesh.HasNormals) {
						FillVertexAttrib (vertexType, VertexAttribute.NORMAL, ref internalMesh.Vertices [i], new Vector3 (mesh.Normals [i].X, mesh.Normals [i].Y, mesh.Normals [i].Z));
					}
				}
				bounds = new BoundingBox (min,max);
				internalMesh.UsageHint = UsageHint.DynamicDraw;
			}
			internalMesh.bounds = bounds;
			return convert?.Invoke(internalMesh) ?? internalMesh;
		}

		private void FillVertexAttrib(Type vertType, VertexAttribute vertAttrib, ref IVertex vert,Vector3 param){
			if (!RegisterAsAttribute.registeredVertexFormats [vertType].ContainsKey (vertAttrib))
				return;
			if (RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers.Count > 1) {
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers [0] (vert, param.X);
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers [1] (vert, param.Y);
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers [2] (vert, param.Z);
			} else
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers [0] (vert, param);
			
		}
		private void FillVertexAttrib(Type vertType, VertexAttribute vertAttrib, ref IVertex vert,Vector2 param){
			if (!RegisterAsAttribute.registeredVertexFormats [vertType].ContainsKey (vertAttrib))
				return;
			if (RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers.Count > 1) {
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers[0] (vert,param.X);
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers[1] (vert,param.Y);
			}
			else
				RegisterAsAttribute.registeredVertexFormats [vertType] [vertAttrib].generatedFillers[0] (vert,param);
		}
	//	public Matrix4 CalculateModelMatrix(){

	//	return Matrix4.Scale(Scale) * Matrix4.CreateRotationX(Rotation.X) * Matrix4.CreateRotationY(Rotation.Y) * Matrix4.CreateRotationZ(Rotation.Z) * Matrix4.CreateTranslation(Position);
		//}
		public override void Export (string pathToExport, string format)
		{
			throw new NotImplementedException ();
		}
	}

}

