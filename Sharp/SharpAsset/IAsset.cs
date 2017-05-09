using System;

using Sharp;
using OpenTK;

namespace SharpAsset
{
    public interface IAsset  //load unload
    {
        string FullPath { get; set; }
        string Extension { get; set; }
        string Name { get; set; }

        //geticon ?
        void PlaceIntoScene(Entity context /*null if placed over void*/, Vector3 worldPos);

        //void PlaceIntoInspector & placeIntoStructure or one to rule them all PlaceIntoView(View view)
    }
}