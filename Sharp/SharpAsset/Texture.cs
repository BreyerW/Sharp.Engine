using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System;
using Sharp.Editor.Views;

namespace SharpAsset
{
	public struct Texture:IAsset
	{
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		internal int TBO;

		public Bitmap bitmap;

        internal static Dictionary<string,Texture> textures=new Dictionary<string, Texture>();

        private bool allocated;

        public static Texture getAsset(string name) {
            var tex = textures[name];
            if (!tex.allocated)
            {
                SceneView.backendRenderer.GenerateBuffers(ref tex);
                SceneView.backendRenderer.BindBuffers(ref tex);
                SceneView.backendRenderer.Allocate(ref tex);
                tex.allocated = true;
                textures[name] = tex;
            }
            return tex;
        }

		public override string ToString ()
		{
			return Name;
		}

        public IAsset GetOrLoadAsset()
        {
            throw new NotImplementedException();
        }
    }
}

