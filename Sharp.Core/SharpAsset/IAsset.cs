using Sharp;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

//[assembly: TypeForwarded(typeof(System.Numerics.Vector3))]
namespace SharpAsset
{
    public interface IAsset  //load unload
    {
        string FullPath { get; set; }//fastSON.JSON fastSON.BSON
        ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } }
        ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } }

        //geticon ?

    }
}