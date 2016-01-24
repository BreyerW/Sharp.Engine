using System.IO;
using System.Collections.Generic;
using System;
using OpenTK;

namespace SharpAsset
{
	public struct Skeleton: IAsset
	{
		internal int VBOV;
		internal int VBOI;
		internal int VAO;
		internal Matrix4 MVP;

		public Bone this[string boneName]{
			get{
				return bones [bonesName [boneName]];
			}
			set{
				if (bones == null) {
					bones = new List<Bone> ();
					bonesName = new Dictionary<string, int> ();
				}
				if (bonesName.ContainsKey (boneName))
					return;
				bones.Add (value);
				bonesName.Add (boneName,bones.Count-1);
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
		public string FullPath {
			get;
			set;
		}
		public string Extension {
			get {
				return Path.GetExtension (FullPath);
			}
			set {
				//throw new NotImplementedException ();
			}
		}
		public string Name {
			get {
				return Path.GetFileNameWithoutExtension (FullPath);
			}
			set {
				//throw new NotImplementedException ();
			}
		}
		#endregion
		
	}
	public class Bone{
		public string Name { get; set; }
		public Matrix4 Offset { get; set; }
		// local matrix transform
		public Matrix4 LocalTransform { get; set; }
		// To-root transform
		public Matrix4 GlobalTransform { get; set; }
		// copy of the original local transform
		public Matrix4 OriginalLocalTransform { get; set; }
		// parent bone reference
		private int idParent;
		public Bone Parent { get; set; }
		// child bone references
		public List<Bone> Children { get; internal set; }
	}
}

