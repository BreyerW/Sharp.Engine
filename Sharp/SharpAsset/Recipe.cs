using System;
using System.IO;
using OpenTK;
using Sharp;

namespace SharpAsset
{
    struct Recipe : IAsset
    {
        public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
        public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }
        public byte[] settings;
        //delta field for nested recipes?
        public void PlaceIntoScene(Entity context, Vector3 worldPos)
        {
            throw new NotImplementedException();
        }
    }
}