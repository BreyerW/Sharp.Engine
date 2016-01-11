using System.Drawing.Imaging;
using OpenTK.Graphics.OpenGL;
using System;
using SharpAsset;

namespace Sharp
{
	public class Material
	{
		public int shaderId;

		static Material ()
		{
			
		}

		public void SetTexture (string propName, Texture tex){
			if(tex.texId==-1)
				tex.texId=GL.GenTexture ();
			//Console.WriteLine ("texId "+tex.texId);
			GL.BindTexture(TextureTarget.Texture2D,0);
			BitmapData data = tex.bitmap.LockBits(new System.Drawing.Rectangle(0, 0, tex.bitmap.Width, tex.bitmap.Height),
				ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

			tex.bitmap.UnlockBits(data);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.ActiveTexture(TextureUnit.Texture0);
			//GL.Uniform1(GL.GetUniformLocation(shaderId, "MyTexture"), TextureUnit.Texture0 - TextureUnit.Texture0);

		}
	}
}

