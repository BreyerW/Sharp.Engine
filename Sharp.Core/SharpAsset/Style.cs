using System;
using System.Collections.Generic;
using System.Numerics;
using Sharp;
using System.IO;

namespace SharpAsset
{
    public struct Style : IAsset
    {
        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }

        public List<IStyleProperty> properties;

        public void PlaceIntoScene(Entity context, Vector3 worldPos)
        {
        }
    }
}