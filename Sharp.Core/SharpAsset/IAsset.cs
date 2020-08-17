using System.Runtime.CompilerServices;
using Sharp;
using System.Numerics;
using System;

//[assembly: TypeForwarded(typeof(System.Numerics.Vector3))]
namespace SharpAsset
{
    public interface IAsset  //load unload
    {
        string FullPath { get; set; }//fastSON.JSON fastSON.BSON
        string Extension { get;/* set;*/ }
        string Name { get; /*set;*/ }

        //geticon ?
        void PlaceIntoScene(Entity context /*null if placed over void*/, Vector3 worldPos);

        //void PlaceIntoInspector & placeIntoStructure or one to rule them all PlaceIntoView(View view)
    }
}