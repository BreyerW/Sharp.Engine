using System;
using Squid;
using Sharp;
using SharpAsset.Pipeline;
using Sharp.Editor.Views;
using SharpFont.HarfBuzz;
using SharpFont;
using OpenTK;
using System.Runtime.CompilerServices;
using System.IO;

namespace SharpSL.BackendRenderers
{
    public class UIRenderer : ISquidRenderer
    {
        private SharpFont.HarfBuzz.Buffer buffer;
        private SharpFont.HarfBuzz.Font hbFont;

        public void DrawBox(int x, int y, int width, int height, int color)//DrawMesh?
        {
            var col = new Color((uint)color);
            MainEditorView.editorBackendRenderer.DrawQuad(x, y, x + width, y + height, ref col.R);
        }

        public void DrawText(string text, int x, int y, int font, int color)//remove that replace with gettexture with special name and render as texture?
        {
            var chars = text.AsSpan();
            MainWindow.backendRenderer.ChangeShader();

            buffer = new SharpFont.HarfBuzz.Buffer();
            buffer.AddText(text);
            ref var face = ref FontPipeline.assets[font].face;
            hbFont = SharpFont.HarfBuzz.Font.FromFTFace(face);
            buffer.Script = SharpFont.HarfBuzz.Script.Common;
            buffer.Direction = Direction.LeftToRight;
            var col = new Color((uint)color);
            hbFont.Shape(buffer);
            var glyphInfos = buffer.GlyphInfo();
            var glyphPositions = buffer.GlyphPositions();

            int height = (face.MaxAdvanceHeight - face.Descender) >> 6;
            int width = 0;
            for (int i = 0; i < glyphInfos.Length; ++i)
            {
                width += glyphPositions[i].xAdvance >> 6;
            }
            MainWindow.backendRenderer.WriteDepth(false);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Blend);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            OpenTK.Graphics.OpenGL.GL.BlendFunc(OpenTK.Graphics.OpenGL.BlendingFactorSrc.SrcAlpha, OpenTK.Graphics.OpenGL.BlendingFactorDest.OneMinusSrcAlpha);
            int penX = 0, penY = face.MaxAdvanceHeight >> 6;

            var mat = Matrix4.CreateTranslation(x, y, 0) * Camera.main.OrthoMatrix;
            MainEditorView.editorBackendRenderer.UnloadMatrix();
            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);
            for (int i = 0; i < chars.Length; ++i)
            {
                //draw the string
                int tbo = -1;
                face.LoadGlyph(face.GetCharIndex(chars[i]), LoadFlags.Default, LoadTarget.Normal);
                face.Glyph.RenderGlyph(RenderMode.Normal);

                var cBmp = face.Glyph.Bitmap.BufferData;

                MainWindow.backendRenderer.GenerateBuffers(ref tbo);
                MainWindow.backendRenderer.BindBuffers(ref tbo);
                int newwidth = MathHelper.NextPowerOfTwo(face.Glyph.Bitmap.Width);
                int newheight = MathHelper.NextPowerOfTwo(face.Glyph.Bitmap.Rows);
                // Allocate Memory For The Texture Data.
                byte[] expanded_data = new byte[2 * newwidth * newheight];

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

                for (int j = 0; j < face.Glyph.Bitmap.Rows; j++)
                    Unsafe.CopyBlockUnaligned(ref expanded_data[j * newwidth], ref cBmp[j * face.Glyph.Bitmap.Width], (uint)face.Glyph.Bitmap.Width);

                MainWindow.backendRenderer.Allocate(ref expanded_data[0], newwidth, newheight, true);
                MainEditorView.editorBackendRenderer.DrawQuad(
                      penX,
                 face.Glyph.Metrics.VerticalBearingY.ToSingle(),
                  penX + face.Glyph.Bitmap.Width,
                 face.Glyph.Bitmap.Rows, ref col.R
                  );

                penX += glyphPositions[i].xAdvance >> 6;
                penY -= glyphPositions[i].yAdvance >> 6;
            }
            MainEditorView.editorBackendRenderer.UnloadMatrix();
            MainWindow.backendRenderer.WriteDepth(false);
        }

        public void DrawTexture(int texture, int x, int y, int width, int height, Rectangle source, int color)
        {
            ref var texture2d = ref Pipeline.GetPipeline<TexturePipeline>().GetAsset(texture);
            var col = new Color((uint)color);

            var mat = Matrix4.CreateTranslation(x, y, 0) * Camera.main.OrthoMatrix;
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.Texture2D);
            MainWindow.backendRenderer.Allocate(ref texture2d.bitmap[0], texture2d.width, texture2d.height);
            MainEditorView.editorBackendRenderer.LoadMatrix(ref mat);

            MainEditorView.editorBackendRenderer.DrawSlicedQuad(0, 0, width, height, (float)source.Left / texture2d.width, (float)source.Right / texture2d.width, (float)source.Top / texture2d.height, (float)source.Bottom / texture2d.height, ref col.R);
            MainEditorView.editorBackendRenderer.UnloadMatrix();
        }

        public void EndBatch(bool final)
        {
        }

        public int GetFont(string name)
        {
            return name == "default" ? 0 : FontPipeline.nameToKey.IndexOf(name);
        }

        public Point GetTextSize(string text, int font)
        {
            ref var f = ref FontPipeline.assets[font];
            var textSize = f.Measure(text);
            return new Point(textSize.width, textSize.height);
        }

        public int GetTexture(string name)
        {
            Pipeline.GetPipeline<TexturePipeline>().Import(@"B:\Sharp.Engine3\Sharp\SharpSL\SharpSL.BackendRenderers\Content\" + name);
            return TexturePipeline.nameToKey.IndexOf(Path.GetFileNameWithoutExtension(name));
        }

        public Point GetTextureSize(int texture)
        {
            ref var texture2d = ref TexturePipeline.assets[texture];
            return new Point(texture2d.width, texture2d.height);
        }

        public void Scissor(int x, int y, int width, int height)
        {
            if (Camera.main is null) return;
            MainWindow.backendRenderer.Clip(x, Camera.main.height - (y + height), width, height);
        }

        public void StartBatch()
        {
            //start shader
            //throw new NotImplementedException();
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