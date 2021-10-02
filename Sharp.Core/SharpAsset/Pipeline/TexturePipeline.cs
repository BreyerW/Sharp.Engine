using PluginAbstraction;
using Sharp.Core;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset.AssetPipeline
{
    [SupportedFiles(".bmp", ".jpg", ".png", ".dds")]
    public class TexturePipeline : Pipeline<Texture>
    {
        public override ref Texture Import(string pathToFile)
        {
            ref var asset = ref base.Import(pathToFile);
            if (Unsafe.IsNullRef(ref asset) is false) return ref asset;

            var texture = new Texture();
            texture.TBO = -1;
            texture.FBO = -1;
            texture.format = TextureFormat.RGBA;
            texture.FullPath = pathToFile;
            //var format=ImageInfo.(pathToFile); 
            var data = PluginManager.textureLoader.Import(pathToFile);

            texture.width = data.width;
            texture.height = data.height;
            texture.bitmap = GC.AllocateUninitializedArray<byte>(data.bitmap.Length);
            Unsafe.CopyBlockUnaligned(ref texture.bitmap[0], ref data.bitmap[0], (uint)data.bitmap.Length);
			Console.WriteLine (BitOperations.IsPow2(texture.width) +" : "+ BitOperations.IsPow2(texture.height));
            
            return ref this[Register(texture)];
        }

        public override void Export(string pathToExport, string format)
        {
            throw new NotImplementedException();
        }

        public override void ApplyAsset(in Texture asset, object context)
        {
            throw new NotImplementedException();
        }
    }
}