using System;
using Assimp;
using Assimp.Configs;
using OpenTK;
using System.Linq;
using System.Collections.Generic;

namespace SharpAsset.Pipeline
{
	public class SkeletonPipeline: Pipeline
	{
		public static readonly SkeletonPipeline singleton=new SkeletonPipeline();
		public Scene scene;
		private Dictionary<string,Assimp.Bone> boneNames;
		private List<Assimp.Node> rootBones;

		public override IAsset Import (string pathToFile)
		{
			
			boneNames = new Dictionary<string, Assimp.Bone>();
			//rootBones = new List<Node> ();
			Assimp.Node rootBone=null;
			foreach (var mesh in scene.Meshes)
				foreach (var bone in mesh.Bones)
					boneNames.Add (bone.Name,bone);
			
			foreach(var boneName in boneNames.Keys){
				var boneNode=scene.RootNode.FindNode (boneName);
				if (boneNode.Parent == null || !boneNames.ContainsKey (boneNode.Parent.Name)) {
					rootBone = boneNode.Parent;
					break;
				}
			}
			var skele = new Skeleton ();
			skele.Name = "_Skeleton";
			skele[rootBone.Name]=CreateBoneTree (ref skele, rootBone,null);
			//bvh_to_vertices (skele[rootBone.Name],);
			Console.WriteLine ("/n Start bone list: /n"+rootBone.Name);
		
			return skele;
		}
	
		void PrintBones(Bone n){
			Console.WriteLine (n.Name);
			foreach(var node in n.Children)
				PrintBones (node);
		}
		private int _i;
		private Bone CreateBoneTree(ref Skeleton skele, Node node, Bone parent) {

			var internalNode = new Bone {
				Name = node.Name, Parent = parent,
			};
			if (boneNames.ContainsKey (node.Name)) {
				boneNames [node.Name].OffsetMatrix.Transpose ();
				internalNode.Offset = FromMatrix (boneNames [node.Name].OffsetMatrix);

			}if (internalNode.Name == "") {
				internalNode.Name = "bone_" + _i++;
			}
			//skele[internalNode.Name] = internalNode;
			var trans = node.Transform;
			trans.Transpose(); //drectx stuff
			internalNode.LocalTransform =FromMatrix(trans);
			internalNode.OriginalLocalTransform = internalNode.LocalTransform;
			CalculateBoneToWorldTransform(internalNode);
			internalNode.Children = new List<Bone> ();
			for (var i = 0; i < node.ChildCount; i++) {
				var child = CreateBoneTree(ref skele,node.Children[i], internalNode);
				if (child != null) {
					internalNode.Children.Add(child);
				}
			}

			return internalNode;
		}

		private static void CalculateBoneToWorldTransform(Bone child) {
			child.GlobalTransform = child.LocalTransform;
			var parent = child.Parent;
			while (parent != null) {
				child.GlobalTransform *= parent.LocalTransform;
				parent = parent.Parent;
			}
		}
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

