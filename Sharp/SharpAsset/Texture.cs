using System.IO;
using System;
using System.Numerics;
using Sharp;

namespace SharpAsset
{
    public struct Texture : IAsset
    {
        //internal bool allocated;
        internal int TBO;

        internal byte[] bitmap;
        public int width;
        public int height;

        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public void PlaceIntoScene(Entity context, Vector3 worldPos)
        {
            throw new NotImplementedException();
        }
    }
}