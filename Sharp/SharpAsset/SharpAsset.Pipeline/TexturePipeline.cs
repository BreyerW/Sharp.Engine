using System;
using FreeImageAPI;
using System.IO;
using Sharp;

namespace SharpAsset.Pipeline
{
    [SupportedFiles(".bmp", ".jpg", ".png")]
    public class TexturePipeline : Pipeline<Texture>
    {
        public TexturePipeline()
        {
        }

        public override ref Texture GetAsset(int index)
        {
            ref var tex = ref this[index];
            if (!tex.allocated)
            {
                MainWindow.backendRenderer.GenerateBuffers(ref tex.TBO);
                MainWindow.backendRenderer.BindBuffers(ref tex.TBO);
                MainWindow.backendRenderer.Allocate(ref tex.bitmap);
                tex.allocated = true;
                //this[index] = tex;
            }
            return ref this[index];
        }

        public override IAsset Import(string pathToFile)
        {
            var name = Path.GetFileNameWithoutExtension(pathToFile);
            if (nameToKey.Contains(name))
                return this[nameToKey.IndexOf(name)];
            var texture = new Texture();
            var dib = FreeImage.LoadEx(pathToFile);
            texture.TBO = -1;
            texture.FullPath = pathToFile;
            texture.bitmap = FreeImage.GetBitmap(dib);
            //Console.WriteLine (IsPowerOfTwo(texture.bitmap.Width) +" : "+IsPowerOfTwo(texture.bitmap.Height));
            nameToKey.Add(name);

            this[nameToKey.IndexOf(name)] = texture;

            return texture;
        }

        public static bool IsPowerOfTwo(int x)
        {
            return (x != 0) && ((x & (x - 1)) == 0);
        }

        public override void Export(string pathToExport, string format)
        {
            throw new NotImplementedException();
        }
    }
}