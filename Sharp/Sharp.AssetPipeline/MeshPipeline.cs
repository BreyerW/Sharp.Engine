using System;
using System.Collections.Generic;
using Assimp;
using Assimp.Configs;
using Sharp.AssetPipeline;
using Sharp.Editor.Attribs;
using OpenTK;
using System.IO;

namespace Sharp.AssetPipeline
{
	[SupportedFileFormats(".fbx",".dae",".obj")]
	public class MeshPipeline:Pipeline
	{
		public static readonly MeshPipeline singleton=new MeshPipeline();

		private static BoundingBox bounds;

		public static List<IVertex> FillVertexData(Assimp.Mesh mesh, BasicVertexFormat baseVertData){
			var vertDatas = new List<IVertex> (mesh.VertexCount);

			var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
			var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

			for (int i = 0; i < mesh.VertexCount; i++) {
				baseVertData.X = mesh.Vertices [i].X;
				baseVertData.Y = mesh.Vertices [i].Y;
				baseVertData.Z = mesh.Vertices [i].Z;
				var tmpVec = new Vector3 (mesh.Vertices [i].X, mesh.Vertices [i].Y, mesh.Vertices [i].Z);
				//Console.WriteLine ((maxtmpVec));
				min = Vector3.ComponentMin(min, tmpVec);
				max = Vector3.ComponentMax(max, tmpVec);
				if (mesh.HasVertexColors (0)) {
					//baseVertData.Color.r = mesh.VertexColorChannels [0] [i].R;
					//baseVertData.Color.g = mesh.VertexColorChannels [0] [i].G;
					//baseVertData.Color.b = mesh.VertexColorChannels [0] [i].B;
					//baseVertData.Color.a = mesh.VertexColorChannels [0] [i].A;
				} else
					//baseVertData.Color = new Color<float> (1, 1, 1, 1);
				if (mesh.HasTextureCoords (0)) {
					baseVertData.texcoords.X = mesh.TextureCoordinateChannels [0] [i].X;
					baseVertData.texcoords.Y =1- mesh.TextureCoordinateChannels [0] [i].Y;
				}
				vertDatas.Add (baseVertData);
			}
			bounds = new BoundingBox (min,max);

			return vertDatas;
		}
		public static Func<Assimp.Mesh,BasicVertexFormat,List<IVertex>> onFillVertexData=FillVertexData;

		public override IAsset Import (string pathToFile)
		{
			var format = Path.GetExtension (pathToFile);
			//if (!SupportedFileFormatsAttribute.supportedFileFormats.Contains (format))
				//throw new NotSupportedException (format+" format is not supported");
			
			var asset = new AssimpContext ();
			var scene=asset.ImportFile (pathToFile,PostProcessPreset.TargetRealTimeMaximumQuality);
			var internalMesh = new Sharp.Mesh<ushort> ();
			internalMesh.FullPath = pathToFile;

			foreach (var mesh in scene.Meshes) {
				//Console.WriteLine ("indices : "+ mesh.GetUnsignedIndices());
				//mesh.MaterialIndex
				internalMesh.Indices=new ushort[mesh.VertexCount*3];
				internalMesh.Indices=(ushort[])(object)mesh.GetShortIndices();
				internalMesh.Vertices = new List<IVertex> ();
				internalMesh.Vertices.AddRange(onFillVertexData (mesh, new BasicVertexFormat ()));

				internalMesh.UsageHint = OpenTK.Graphics.OpenGL.BufferUsageHint.DynamicDraw;
			}
			internalMesh.bounds = bounds;
			return internalMesh;
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

