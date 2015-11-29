using System.IO;
using System.Drawing;
using System.Collections.Generic;

namespace Sharp
{
	public struct Texture:IAsset
	{
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		internal int texId;
		public Bitmap bitmap;

		public static Dictionary<string,Texture> textures=new Dictionary<string, Texture>();

		public override string ToString ()
		{
			return Name;
		}
	}
}

