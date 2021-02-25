using System;
using Squid;
using Sharp;
using SharpAsset.Pipeline;
using Sharp.Editor.Views;
using System.Numerics;
using SharpAsset;
using Sharp.Editor;
using System.Runtime.InteropServices;
using Font = SharpAsset.Font;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.Shapes;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace SharpSL.BackendRenderers
{
	public class UIRenderer : ISquidRenderer
	{
		private static int currentFont = -1;
		//Typeface face = default;
		//private static StbTrueType.stbtt_fontinfo face = new StbTrueType.stbtt_fontinfo();
		private static SixLabors.Fonts.Font face;
		private static Material sdfMaterial;
		private static Material squareMaterial;
		private static Material texturedSquareMaterial;
		private static Sharp.Color fontColor = Sharp.Color.White;
		static UIRenderer()
		{
			var shader = (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\SDFShader.shader");
			sdfMaterial = new Material();
			sdfMaterial.BindShader(0, shader);
			sdfMaterial.BindProperty("color", fontColor);
			sdfMaterial.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("square"));

			squareMaterial = new Material();
			squareMaterial.BindShader(0, (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\ColoredEditorShader.shader"));
			squareMaterial.BindProperty("mesh", Pipeline.Get<Mesh>().GetAsset("square"));
			squareMaterial.BindProperty("len", Vector2.One);

			var square = Pipeline.Get<Mesh>().GetAsset("dynamic_square");

			texturedSquareMaterial = new Material();
			texturedSquareMaterial.BindShader(0, (Shader)Pipeline.Get<Shader>().Import(Application.projectPath + @"\Content\TexturedEditorShader.shader"));
			texturedSquareMaterial.BindProperty("mesh", square);
		}
		public void DrawBox(int x, int y, int width, int height, int color)//DrawMesh?
		{
			var col = new Sharp.Color((uint)color);
			var mat = Matrix4x4.CreateScale(width, height, 1) * Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoMatrix;
			squareMaterial.BindProperty("model", mat);
			squareMaterial.BindProperty("color", col);
			squareMaterial.Draw();
		}
		//float PointToPixelSize(float pt)
		//{
		//return pt / resolution * dpi;
		//}

		float PixelToPointSize(float px)
		{
			return (px * face.Size) / (face.EmSize);
		}
		public void DrawText(string text, int x, int y, int width, int height, int font, int color, float fontSize)//TODO: split this to draw texture and draw mesh
		{
			var chars = text.AsSpan();
			ref var realFont = ref Pipeline.Get<Font>().GetAsset(font);
			var col = new Sharp.Color((uint)color);
			float penX = 0, penY = PixelToPointSize(face.Ascender + face.Descender);
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
				if (chars[i] != ' ')
				{
					if (!realFont.metrics.ContainsKey(chars[i]))
						GenerateTextureForChar(chars[i], ref realFont);

					var metrics = realFont.metrics[chars[i]];
					var texChar = metrics.tex;


					var bottomLeft = (float)Math.Floor((penX) * scale.x);
					var topLeft = (float)Math.Floor((penY) * scale.y);
					var topRight = texChar.height;
					var bottomRight = texChar.width;
					var mat = Matrix4x4.CreateScale(bottomRight, topRight, 0) * Matrix4x4.CreateTranslation(x + bottomLeft, y + topLeft, 0) * MainEditorView.currentMainView.camera.OrthoMatrix;

					sdfMaterial.BindProperty("model", mat);
					sdfMaterial.BindProperty("msdf", texChar);
					sdfMaterial.Draw();
				}
				var m = realFont.metrics[chars[i]];
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
				penX += m.advance + 1; //2 == Spacing? same as Metrics.HorizontalAdvance?
									   //penY += m.advance.Y;

				#region Kerning (for NEXT character)

				// Adjust for kerning between this character and the next.
				var kerning = i is 0 ? 0 : face.Instance.GetOffset(face.GetGlyph(chars[i]).Instance, face.GetGlyph(chars[i - 1]).Instance).X;
				//kern = 0;
				penX += kerning / 100f; //TODO: figure out number to scale kerning

				#endregion Kerning (for NEXT character)

				#endregion Draw glyph
			}

		}

		private void GenerateTextureForChar(char c, ref Font f)
		{
			if (currentFont is -1 || f.Name.SequenceEqual(Pipeline.Get<Font>().GetAsset(currentFont).Name))
			{
				FontFamily fam = SystemFonts.Find("Arial");
				face = new SixLabors.Fonts.Font(fam, 18); // size doesn't matter too much as we will be scaling shortly anyway

				//face = new OpenFontReader().Read();
				/*	var fontFile = new FileStream(f.FullPath, FileMode.Open, FileAccess.Read);
				var fileBytes = new byte[fontFile.Length];
				int b;
				int i = 0;
			while ((b = fontFile.ReadByte()) > -1)
				{
					fileBytes[i] = (byte)b;
					i++;
				}
				fixed (byte* addr = &fileBytes[0])
				{
					var succ = StbTrueType.stbtt_InitFont(face, addr, 0);
				}*/
				currentFont = FontPipeline.nameToKey.IndexOf(f.Name.ToString());
			}
			Texture tex = default;
			/*var glyph = face.Lookup(c);
			var builder = new GlyphPathBuilder(face);
			advance = face.GetHAdvanceWidthFromGlyphIndex(glyph.GlyphIndex);
			bearingX = face.GetHFrontSideBearingFromGlyphIndex(glyph.GlyphIndex);*/
			//TextMeasurer.TryMeasureCharacterBounds(MemoryMarshal.CreateReadOnlySpan(ref c, 1), style, out var bounds);
			var glyph = face.GetGlyph(c);
			var origin = Vector2.Zero;
			var nextPowerOfTwo = (int)Math.Ceiling(Math.Pow(2, (int)Math.Log(18, 2) + 1));
			if (c != ' ')
			{
				/*	float pxscale = face.CalculateScaleToPixelFromPointSize(24);
					builder.BuildFromGlyphIndex(glyph.GlyphIndex, -1);
						var glyphContourBuilder = new ContourBuilder();
						var genParams = new MsdfGenParams();
					genParams.shapeScale = pxscale;
						builder.ReadShapes(glyphContourBuilder);
						GlyphImage glyphImg = MsdfGlyphGen.CreateMsdfImage(glyphContourBuilder, genParams);
						var s = glyphImg.GetImageBuffer().AsSpan();
						var bitmap = MemoryMarshal.AsBytes(s);// new byte[width * height * 3];

					int i = 0, x = 0, y = 0;
					/*while (i < bitmap.Length)
					{
						var pixel = fbitmap.GetPixel(x, y);
						bitmap[i] = (byte)Math.Clamp(256.0f * pixel.r, 0.0f, 255.0f);
						bitmap[i + 1] = (byte)Math.Clamp(256.0f * pixel.g, 0.0f, 255.0f);
						bitmap[i + 2] = (byte)Math.Clamp(256.0f * pixel.b, 0.0f, 255.0f);

						x++;
						if (x == width)
						{
							x = 0;
							y++;
						}
						i += 3;
					}*/


				/*var index = StbTrueType.stbtt_FindGlyphIndex(face, codepoint);
				var bytes = StbTrueType.stbtt_GetGlyphBitmap(face, 0.1f, 0.1f, index, &width, &height, &xoff, &yoff);
				StbTrueType.stbtt_GetCodepointHMetrics(face, codepoint, &advance, &bearingX);*/
				//var widthScale = (24.0f / glyphs.Bounds.Width);
				//var heightScale = (24.0f/ glyphs.Bounds.Height);
				//var minScale = Math.Min(widthScale, heightScale);
				var invalidchars = System.IO.Path.GetInvalidFileNameChars();
				//glyphs = glyphs.Scale(minScale);

				//glyphs = glyphs.Translate(-glyphs.Bounds.Location.X, 0);
				bool allowed = true;
				foreach (var invalid in invalidchars)
				{
					allowed = invalid != c;
				}
				using Image<A8> img = new Image<A8>(Configuration.Default, nextPowerOfTwo, nextPowerOfTwo, new A8(0));
				img.Mutate(i => i.DrawText("" + c, face, SixLabors.ImageSharp.Color.White, new PointF(0f, -3f)));
				img.TryGetSinglePixelSpan(out var span);
				tex = new Texture()
				{
					FullPath = c + "_" + f.Name.ToString() + ".generated",
					TBO = -1,
					FBO = -1,
					format = TextureFormat.A,
					bitmap = MemoryMarshal.AsBytes(span).ToArray(),
					width = img.Width,
					height = img.Height
				};
				//var bitmap = new Span<byte>(bytes, width * height).ToArray();

				Pipeline.Get<Texture>().Register(tex);
			}
			f.metrics.Add(c, (tex, PixelToPointSize(glyph.Instance.LeftSideBearing), PixelToPointSize(glyph.Instance.AdvanceWidth)));

		}

		public void DrawTexture(int texture, int x, int y, int width, int height, Squid.Rectangle source, int color)//get into account slicing offset
		{
			ref var texture2d = ref Pipeline.Get<Texture>().GetAsset(texture);
			var col = new Sharp.Color((uint)color);

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
			OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);
			OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
			OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
			OpenTK.Graphics.OpenGL.GL.BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactor.SrcAlpha, OpenTK.Graphics.OpenGL.BlendingFactor.OneMinusSrcAlpha);
		}

		public void EndBatch(bool final)//OnPostRender
		{
			OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);
			//MainWindow.backendRenderer.BindBuffers(Target.Texture, 0);
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
				if (!f.metrics.ContainsKey(c))
					GenerateTextureForChar(c, ref f);
				var g = f.metrics[c];
				var kerning = i is 0 ? 0 : face.Instance.GetOffset(face.GetGlyph(span[i]).Instance, face.GetGlyph(span[i - 1]).Instance).X;

				xoffset += kerning / 100f + g.advance + 1;
				newHeight = g.tex.height + yoffset;
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
			MainWindow.backendRenderer.Clip(x, MainEditorView.currentMainView.camera.height - (y + height), width, height);
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