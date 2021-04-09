using System;
using Squid;
using Sharp;
using SharpAsset.AssetPipeline;
using Sharp.Editor.Views;
using System.Numerics;
using SharpAsset;
using Sharp.Editor;
using Font = SharpAsset.Font;
using Sharp.Core;
using PluginAbstraction;

namespace SharpSL.BackendRenderers
{
	public class UIRenderer : ISquidRenderer
	{
		private static Material sdfMaterial;
		private static Material squareMaterial;
		private static Material texturedSquareMaterial;
		private static Color fontColor = Color.White;
		static UIRenderer()
		{
			var shader = Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\SDFShader.shader");
			sdfMaterial = new Material();
			sdfMaterial.BindShader(0, shader);
			sdfMaterial.BindProperty("color", fontColor);
			sdfMaterial.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("square"));

			squareMaterial = new Material();
			squareMaterial.BindShader(0, Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\ColoredEditorShader.shader"));
			squareMaterial.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("square"));
			squareMaterial.BindProperty("len", Vector2.One);

			var square = Pipeline.Get<Mesh>().GetAsset("dynamic_square");

			texturedSquareMaterial = new Material();
			texturedSquareMaterial.BindShader(0, Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\TexturedEditorShader.shader"));
			texturedSquareMaterial.BindProperty("mesh", square);
		}
		public void DrawBox(int x, int y, int width, int height, int color)//DrawMesh?
		{
			var col = new Color((uint)color);
			var mat = Matrix4x4.CreateScale(width, height, 1) * Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoMatrix;
			squareMaterial.BindProperty("model", mat);
			squareMaterial.BindProperty("color", col);
			squareMaterial.Draw();
		}
		//float PointToPixelSize(float pt)
		//{
		//return pt / resolution * dpi;
		//}

		float PixelToPointSize(float px, Font f)
		{
			return (px * f.Size) / (f.EmSize);
		}
		public void DrawText(string text, int x, int y, int width, int height, int font, int color, float fontSize)//TODO: split this to draw texture and draw mesh
		{
			var chars = text.AsSpan();
			ref var realFont = ref Pipeline.Get<Font>().GetAsset(font);
			var col = new Color((uint)color);
			float penX = 0, penY = PixelToPointSize(realFont.Ascender + realFont.Descender, realFont);
			float stringWidth = 0; // the measured width of the string
			float stringHeight = 0; // the measured height of the string
			float overrun = 0;
			float underrun = 0;
			float kern = 0;
			underrun = 0;
			overrun = 0;
			for (int i = 0; i < chars.Length; i++)
			{

				#region Load character
				(float x, float y) scale = (1, 1);

				#endregion Load character

				#region Draw glyph


				#region Underrun

				// Underrun
				//underrun += texChar.bearing.x * scale.x;
				//if (penX == 0)
				//penX += underrun;
				/* if (underrun <= 0)
                 {
                     underrun = 0;
                 }*/

				#endregion Underrun

				var fontData = realFont[chars[i]];
				var metrics = fontData.metrics;
				if (chars[i] != ' ')
				{

					var texChar = fontData.texture;


					var bottomLeft = (float)Math.Floor((penX) * scale.x);
					var topLeft = (float)Math.Floor((penY) * scale.y);
					var topRight = texChar.height;
					var bottomRight = texChar.width;
					var mat = Matrix4x4.CreateScale(bottomRight, topRight, 0) * Matrix4x4.CreateTranslation(x + bottomLeft, y + topLeft, 0) * MainEditorView.currentMainView.camera.OrthoMatrix;

					sdfMaterial.BindProperty("model", mat);
					sdfMaterial.BindProperty("msdf", texChar);
					sdfMaterial.Draw();
				}
				#region Overrun

				//if (texChar.bearing.x + texChar.texture.width > 0 || texChar.advance.x > 0)
				//{
				//   overrun -= Math.Max(texChar.bearing.x + texChar.texture.width, texChar.advance.x);
				//   if (overrun <= 0) overrun = 0;
				//}
				//overrun += (float)(texChar.bearing.x == 0 && texChar.texture.width == 0 ? 0 : (texChar.bearing.x + texChar.texture.width - texChar.advance.x));
				//if (i == text.Length - 1)
				//penX += overrun;

				#endregion Overrun

				// Advance pen positions for drawing the next character.
				penX += PixelToPointSize(metrics.Advance, realFont) + 1; //2 == Spacing? same as Metrics.HorizontalAdvance?
																		 //penY += m.advance.Y;

				#region Kerning (for NEXT character)

				// Adjust for kerning between this character and the next.
				var kerning = i is 0 ? 0 : realFont.GetKerningData(chars[i], chars[i - 1]).X;
				//kern = 0;
				penX += kerning / 100f; //TODO: figure out number to scale kerning

				#endregion Kerning (for NEXT character)

				#endregion Draw glyph
			}

		}

		public void DrawTexture(int texture, int x, int y, int width, int height, Squid.Rectangle source, int color)//get into account slicing offset
		{
			ref var texture2d = ref Pipeline.Get<Texture>().GetAsset(texture);
			var col = new Color((uint)color);

			var mat = Matrix4x4.CreateScale(width, height, 1) * Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoMatrix;

			ref var mesh = ref Pipeline.Get<Mesh>().GetAsset("dynamic_square");
			if (source.Left > 0 || source.Right > 0 || source.Top > 0 || source.Bottom > 0)
			{
				mesh.ReadVertexAtIndex<UIVertexFormat>(0).texcoords = new Vector2((float)source.Left / texture2d.width, (float)source.Top / texture2d.height);
				mesh.ReadVertexAtIndex<UIVertexFormat>(1).texcoords = new Vector2((float)source.Left / texture2d.width, (float)source.Bottom / texture2d.height);
				mesh.ReadVertexAtIndex<UIVertexFormat>(2).texcoords = new Vector2((float)source.Right / texture2d.width, (float)source.Bottom / texture2d.height);
				mesh.ReadVertexAtIndex<UIVertexFormat>(3).texcoords = new Vector2((float)source.Right / texture2d.width, (float)source.Top / texture2d.height);

			}
			else
			{
				mesh.ReadVertexAtIndex<UIVertexFormat>(0).texcoords = new Vector2(0, 0);
				mesh.ReadVertexAtIndex<UIVertexFormat>(1).texcoords = new Vector2(0, 1);
				mesh.ReadVertexAtIndex<UIVertexFormat>(2).texcoords = new Vector2(1, 1);
				mesh.ReadVertexAtIndex<UIVertexFormat>(3).texcoords = new Vector2(1, 0);
			}
			texturedSquareMaterial.BindProperty("model", mat);
			texturedSquareMaterial.BindProperty("tex", texture2d);
			texturedSquareMaterial.BindProperty("tint", col);
			texturedSquareMaterial.Draw();
		}

		public void StartBatch()//OnPreRender
		{
			//start shader
			PluginManager.backendRenderer.DisableState(RenderState.DepthTest);
		}

		public void EndBatch(bool final)//OnPostRender
		{
			PluginManager.backendRenderer.EnableState(RenderState.DepthTest);
		}

		public int GetFont(string name)
		{
			return name is "default" ? 0 : FontPipeline.nameToKey.IndexOf(name);
		}

		public Squid.Point GetTextSize(string text, int font, float fontSize, int position = -1)
		{
			ref var f = ref Pipeline.Get<Font>().GetAsset(font);
			var v = Vector2.Zero;
			float xoffset = 0;
			float yoffset = 0;
			float newHeight = 0;
			int i = 0;
			var span = text.AsSpan();

			foreach (var c in text)
			{
				if (position == i) break;
				if (c == '\n')
				{
					yoffset += 3;
					xoffset = 0;
					continue;
				}
				var fontData = f[c];
				var g = fontData.metrics;
				var kerning = i is 0 ? 0 : f.GetKerningData(span[i], span[i - 1]).X;

				xoffset += kerning / 100f + PixelToPointSize(g.Advance, f) + 1;
				newHeight = fontData.texture.height + yoffset;
				if (newHeight > v.Y)
				{
					v.Y = newHeight;
				}
				if (xoffset > v.X) v.X = xoffset;
				i++;
			}
			return new Squid.Point((int)v.X, (int)v.Y);
		}

		public int GetTexture(string name)
		{
			Pipeline.Get<Texture>().Import(Application.projectPath + @"\SharpSL\BackendRenderers\Content\" + name);
			return TexturePipeline.nameToKey.IndexOf(System.IO.Path.GetFileNameWithoutExtension(name));
		}

		public Squid.Point GetTextureSize(int texture)
		{
			ref var texture2d = ref Pipeline.Get<Texture>().GetAsset(texture);
			return new Squid.Point(texture2d.width, texture2d.height);
		}

		public void Scissor(int x, int y, int width, int height)
		{
			PluginManager.backendRenderer.Clip(x, MainEditorView.currentMainView.camera.height - (y + height), width, height);
		}

		#region IDisposable Support

		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~UIOpenGLRenderer() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

		#endregion IDisposable Support
	}
}