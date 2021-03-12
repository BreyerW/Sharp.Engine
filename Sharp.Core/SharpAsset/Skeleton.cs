using Sharp;
using SharpAsset.AssetPipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SharpAsset
{
	public struct Skeleton : IAsset
	{
		internal int VBOV;
		internal int VBOI;
		internal int VAO;
		internal Matrix4x4 MVP;
		public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
		public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } set { } }

		public Bone this[string boneName]
		{
			get
			{
				return bones[bonesName[boneName]];
			}
			set
			{
				if (bones == null)
				{
					bones = new List<Bone>();
					bonesName = new Dictionary<string, int>();
				}
				if (bonesName.ContainsKey(boneName))
					return;
				bones.Add(value);
				bonesName.Add(boneName, bones.Count - 1);
			}
		}

		/*private int rootBone;
		public Bone RootBone{
			get{
				return bones [rootBone];
			}
			set{
				if (bones.Contains (value))
					rootBone = bones.IndexOf (value);
				else
					throw new ArgumentException ("Tried to apply root bone which doesn't exist in "+Name+" skeleton");
			}
		}*/

		public List<Bone> bones;
		public Dictionary<string, int> bonesName;

		#region IAsset implementation

		public string FullPath
		{
			get;
			set;
		}

		#endregion IAsset implementation
	}

	public class Bone
	{
		public string Name { get; set; }
		public Matrix4x4 Offset { get; set; }

		// local matrix transform
		public Matrix4x4 LocalTransform { get; set; }

		// To-root transform
		public Matrix4x4 GlobalTransform { get; set; }

		// copy of the original local transform
		public Matrix4x4 OriginalLocalTransform { get; set; }

		// parent bone reference
		private int idParent;

		public Bone Parent { get; set; }

		// child bone references
		public List<Bone> Children { get; internal set; }
	}
}