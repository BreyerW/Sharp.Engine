using System.IO;
using System.Collections.Generic;
using System.Drawing;
using System;
using Sharp.Editor.Views;
using OpenTK;
using Sharp;

namespace SharpAsset
{
    public struct Texture : IAsset
    {
        internal bool allocated;
        internal int TBO;
        internal Bitmap bitmap;

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