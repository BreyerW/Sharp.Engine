using System;
using System.IO;
using System.Numerics;
using Sharp;

namespace SharpAsset
{
    internal struct Recipe : IAsset
    {
        //public string Name { get { return Path.GetFileNameWithoutExtension(FullPath); } set { } }
       // public string Extension { get { return Path.GetExtension(FullPath); } set { } }
        public string FullPath { get; set; }
        public byte[] settings;

        //delta field for nested recipes?

    }
}