using System;
using FreeImageAPI;
using System.IO;
using Sharp;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpAsset.Pipeline
{
    [SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
    public class TexturePipeline : Pipeline<Texture>
    {
        public TexturePipeline()
        {
        }

        public override ref Texture GetAsset(int index)
        {
            ref var tex = ref this[index];
            if (tex.TBO is -1)
            {
                Console.WriteLine("allocate");
                MainWindow.backendRenderer.GenerateBuffers(ref tex.TBO);
                MainWindow.backendRenderer.BindBuffers(ref tex.TBO);
                MainWindow.backendRenderer.Allocate(ref tex.bitmap[0], tex.width, tex.height);
                //tex.allocated = true;
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
            var dib = FreeImage.ConvertTo32Bits(FreeImage.LoadEx(pathToFile));
            var scanwidth = FreeImage.GetPitch(dib);
            texture.width = (int)FreeImage.GetWidth(dib);
            texture.height = (int)FreeImage.GetHeight(dib);
            texture.TBO = -1;
            texture.FullPath = pathToFile;
            texture.bitmap = new byte[scanwidth * texture.height];
            FreeImage.ConvertToRawBits(texture.bitmap, dib, (int)scanwidth, 32, FreeImage.FI_RGBA_RED_MASK, FreeImage.FI_RGBA_GREEN_MASK, FreeImage.FI_RGBA_BLUE_MASK, true);
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