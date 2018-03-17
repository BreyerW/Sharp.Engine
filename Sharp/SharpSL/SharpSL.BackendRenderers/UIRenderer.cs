using System;
using Squid;
using Sharp;
using SharpAsset.Pipeline;
using Sharp.Editor.Views;
using SharpFont;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.IO;

namespace SharpSL.BackendRenderers
{
    public class UIRenderer : ISquidRenderer
    {
        private int currentFace = -1;

        public void DrawBox(int x, int y, int width, int height, int color)//DrawMesh?
        {
            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            var col = new Color((uint)color);
            var mat = Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoLeftBottomMatrix;
            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);
            MainEditorView.editorBackendRenderer.DrawQuad(0, 0, width, height, ref col.R);
            MainEditorView.editorBackendRenderer.UnloadMatrix();
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
        }

        /*  public void DrawMesh(Point[] points, int color)
          {//generalize it for UV and all kinds of meshes
              var mat = MainEditorView.currentMainView.camera.OrthoLeftBottomMatrix;
              MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);
              var col = new Color((uint)color);
              for (int i = 0; i < points.Length - 1; i++)
              {
                  var start = points[i];
                  var end = points[i + 1];
                  MainEditorView.editorBackendRenderer.DrawLine(start.x, start.y, 0, end.x, end.y, 0, ref col.R);
              }
          }*/

        public void DrawText(string text, int x, int y, int width, int height, int font, int color, float fontSize)//TODO: split this to draw texture and draw mesh
        {
            //fontSize = 16;
            var chars = text.AsReadOnlySpan();
            ref var realFont = ref Pipeline.GetPipeline<FontPipeline>().GetAsset(font);
            ref var face = ref realFont.face;
            //MainWindow.backendRenderer.ChangeShader();
            var col = new Color((uint)color);
            float penX = 0, penY = 0;
            float stringWidth = 0; // the measured width of the string
            float stringHeight = 0; // the measured height of the string
            float overrun = 0;
            float underrun = 0;
            float kern = 0;
            // Bottom and top are both positive for simplicity.
            // Drawing in .Net has 0,0 at the top left corner, with positive X to the right
            // and positive Y downward.
            // Glyph metrics have an origin typically on the left side and at baseline
            // of the visual data, but can draw parts of the glyph in any quadrant, and
            // even move the origin (via kerning).
            float top = 0, bottom = 0;

            // Measure the size of the string before rendering it. We need to do this so
            // we can create the proper size of bitmap (canvas) to draw the characters on.
            var size = GetTextSize(text, font, 0);
            stringWidth = size.x;
            stringHeight = size.y;
            // If any dimension is 0, we can't create a bitmap
            if (stringWidth == 0 || stringHeight == 0)
                return;

            MainWindow.backendRenderer.WriteDepth(false);

            var mat = Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoLeftBottomMatrix;
            MainEditorView.editorBackendRenderer.UnloadMatrix();
            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);
            // Create a new bitmap that fits the string.
            underrun = 0;
            overrun = 0;

            // Draw the string into the bitmap.
            // A lot of this is a repeat of the measuring steps, but this time we have
            // an actual bitmap to work with (both canvas and bitmaps in the glyph slot).
            for (int i = 0; i < text.Length; i++)
            {
                #region Load character

                char c = text[i];

                // Same as when we were measuring, except RenderGlyph() causes the glyph data
                // to be converted to a bitmap.
                uint glyphIndex = face.GetCharIndex(c);
                //face.LoadGlyph(glyphIndex, LoadFlags.NoScale, LoadTarget.Normal);
                //face.Glyph.RenderGlyph(RenderMode.Normal);

                //float gAdvanceX = (float)face.Glyph.Advance.X;
                //float gBearingX = (float)face.Glyph.Metrics.HorizontalBearingX;
                var metrics = face.Size;
                (float x, float y) scale = (metrics.Metrics.ScaleX.ToSingle(), metrics.Metrics.ScaleY.ToSingle());
                metrics.Dispose();

                #endregion Load character

                #region Draw glyph

                //int x = ;
                //int y = (int)Math.Round(penY + top - (float)face.Glyph.Metrics.HorizontalBearingY);
                if (!realFont.fontAtlas.ContainsKey(chars[i]))
                    realFont.GenerateBitmapForChar(chars[i]);

                // Whitespace characters sometimes have a bitmap of zero size, but a non-zero advance.
                // We can't draw a 0-size bitmap, but the pen position will still get advanced (below).
                //draw the string
                var texChar = realFont.fontAtlas[chars[i]];

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
                    MainWindow.backendRenderer.Allocate(ref texChar.texture.bitmap[0], texChar.texture.width, texChar.texture.height, true);

                    MainEditorView.editorBackendRenderer.DrawTexturedQuad(
                           (int)Math.Floor(penX * scale.x),
                        (int)Math.Floor(stringHeight + (penY - texChar.bearing.y) * scale.y),
                      (int)Math.Ceiling((penX + texChar.texture.width) * scale.x),
                      (int)Math.Ceiling(stringHeight + (penY - texChar.bearing.y + texChar.texture.height) * scale.y), ref col.R
                      );
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
                penX += texChar.advance.x + 2; // same as Metrics.HorizontalAdvance?
                penY += texChar.advance.y;

                #region Kerning (for NEXT character)

                // Adjust for kerning between this character and the next.
                if (face.HasKerning && i < text.Length - 1)
                {
                    char cNext = text[i + 1];
                    kern = (float)face.GetKerning(glyphIndex, face.GetCharIndex(cNext), KerningMode.Unscaled).X;
                    if (kern > texChar.advance.x * 5 || kern < -(texChar.advance.x * 5))
                        kern = 0;
                    penX += (float)kern;
                }

                #endregion Kerning (for NEXT character)

                #endregion Draw glyph
            }

            MainEditorView.editorBackendRenderer.UnloadMatrix();
            MainWindow.backendRenderer.WriteDepth(true);
        }

        // Here We Fill In The Data For The Expanded Bitmap.
        // Notice That We Are Using A Two Channel Bitmap (One For
        // Channel Luminosity And One For Alpha), But We Assign
        // Both Luminosity And Alpha To The Value That We
        // Find In The FreeType Bitmap.
        // We Use The ?: Operator To Say That Value Which We Use
        // Will Be 0 If We Are In The Padding Zone, And Whatever
        // Is The FreeType Bitmap Otherwise.
        /*  for (int j = 0; j < newheight; j++)
          {
              for (int id = 0; id < newwidth; id++)
              {
                  expanded_data[2 * (id + j * newwidth)] = expanded_data[2 * (id + j * newwidth) + 1] =
                      (id >= face.Glyph.Bitmap.Width || j >= face.Glyph.Bitmap.Rows) ?
                      (byte)0 : cBmp[id + face.Glyph.Bitmap.Width * j];
              }
          }*/

        public void DrawTexture(int texture, int x, int y, int width, int height, Rectangle source, int color)//get into account slicing offset
        {
            ref var texture2d = ref Pipeline.GetPipeline<TexturePipeline>().GetAsset(texture);
            var col = new Color((uint)color);

            var mat = Matrix4x4.CreateTranslation(x, y, 0) * MainEditorView.currentMainView.camera.OrthoLeftBottomMatrix;

            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);
            MainWindow.backendRenderer.Allocate(ref texture2d.bitmap[0], texture2d.width, texture2d.height);
            if (source.Left > 0 || source.Right > 0 || source.Top > 0 || source.Bottom > 0)
                MainEditorView.editorBackendRenderer.DrawSlicedQuad(0, 0, width, height, (float)source.Left / texture2d.width, (float)source.Right / texture2d.width, (float)source.Top / texture2d.height, (float)source.Bottom / texture2d.height, ref col.R);
            else
                MainEditorView.editorBackendRenderer.DrawTexturedQuad(0, 0, width, height, ref col.R);
            MainEditorView.editorBackendRenderer.UnloadMatrix();
        }

        public void StartBatch()//OnPreRender
        {
            //start shader
            OpenTK.Graphics.OpenGL.GL.Disable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
            OpenTK.Graphics.OpenGL.GL.BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL.BlendingFactorDest.OneMinusSrcAlpha);
        }

        public void EndBatch(bool final)//OnPostRender
        {
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.DepthTest);
            OpenTK.Graphics.OpenGL.GL.BindTexture(OpenTK.Graphics.OpenGL.TextureTarget.Texture2D, 0);
        }

        public int GetFont(string name)
        {
            return name == "default" ? 0 : FontPipeline.nameToKey.IndexOf(name);
        }

        public Point GetTextSize(string text, int font, float fontSize, int position = -1)
        {
            ref var f = ref Pipeline.GetPipeline<FontPipeline>().GetAsset(font);
            var textSize = f.Measure(text, position);
            return new Point(textSize.width, textSize.height);
        }

        public int GetTexture(string name)
        {
            Pipeline.GetPipeline<TexturePipeline>().Import(@"B:\Sharp.Engine3\Sharp\SharpSL\SharpSL.BackendRenderers\Content\" + name);
            return TexturePipeline.nameToKey.IndexOf(Path.GetFileNameWithoutExtension(name));
        }

        public Point GetTextureSize(int texture)
        {
            ref var texture2d = ref Pipeline.GetPipeline<TexturePipeline>().GetAsset(texture);
            return new Point(texture2d.width, texture2d.height);
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