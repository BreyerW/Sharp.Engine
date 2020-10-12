﻿using System.Runtime.CompilerServices;
using Sharp;
using System.Numerics;
using System;
using System.IO;

//[assembly: TypeForwarded(typeof(System.Numerics.Vector3))]
namespace SharpAsset
{
	public interface IAsset  //load unload
	{
		string FullPath { get; set; }//fastSON.JSON fastSON.BSON
		ReadOnlySpan<char> Name { get { return Path.GetFileNameWithoutExtension(FullPath); } }
		ReadOnlySpan<char> Extension { get { return Path.GetExtension(FullPath); } }

		//geticon ?
		void PlaceIntoScene(Entity context /*null if placed over void*/, Vector3 worldPos);

		//void PlaceIntoInspector & placeIntoStructure or one to rule them all PlaceIntoView(View view)
	}
}