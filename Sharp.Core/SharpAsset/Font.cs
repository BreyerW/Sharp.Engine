using PluginAbstraction;
using Sharp;
using Sharp.Core;
using SharpAsset.AssetPipeline;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SharpAsset
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Font : IAsset
    {
        private Dictionary<char, CharData> charDictionary;//change to slim dict?

        public CharData this[char c]
        {
            get
            {
                if (charDictionary.TryGetValue(c, out var data))
                {
                    return data;
                }
                else
                {
                    var mets = PluginManager.fontLoader.LoadMetrics(Name.ToString(), Size, c);
                    var tex = PluginManager.fontLoader.GenerateTextureForChar(Name.ToString(), Size, c);
                    var newTex = new Texture()
                    {
                        FullPath = c + "_" + Name.ToString() + ".generated",
                        format = TextureFormat.A,
                        width = tex.width,
                        height = tex.height,
                        data = tex.bitmap,
                        TBO = -1,
                        FBO = -1,
                    };
                    TexturePipeline.Register(newTex);
                    data = new CharData() { metrics = mets, texture = newTex };
                    charDictionary.Add(c, data);
                    return data;
                }
            }
        }

        public string FullPath { get; set; }
        public ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath.AsSpan()); } set { } }
        public ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath.AsSpan()); } set { } }

        public float Size { get; set; }//TODO: reset ascender and descender on size change?
        public ushort EmSize;
        public short Ascender;
        public short Descender;
        public Font(float size)
        {
            Size = size;
            EmSize = default;
            Ascender = default;
            Descender = default;
            charDictionary = new Dictionary<char, CharData>();
            FullPath = "";
        }
        public Vector2 GetKerningData(char c, char next)
        {
            return PluginManager.fontLoader.LoadKerning(Name.ToString(), Size, c, next);
        }
    }
    public struct CharData
    {
        public Texture texture;
        public (float Bearing, float Advance) metrics;
    }

}
