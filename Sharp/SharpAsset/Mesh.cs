﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using Sharp;

namespace SharpAsset
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Mesh<IndexType>:IAsset where IndexType :struct, IConvertible
	{
		public IVertex[] Vertices;
		public string Name{ get{return Path.GetFileNameWithoutExtension (FullPath);  } set{ }}
		public string Extension{ get{return Path.GetExtension (FullPath);  } set{ }}
		public string FullPath{ get; set;}

		internal static IndiceType indiceType = IndiceType.UnsignedByte;

		public IndexType[] Indices;

		internal int VBO;
		internal int EBO;
		//public int TBO;

		public UsageHint UsageHint;

		public BoundingBox bounds;

		static Mesh(){
			if (typeof(IndexType)==typeof(ushort))
				indiceType = IndiceType.UnsignedShort;
			else if (typeof(IndexType)==typeof(uint)) 
				indiceType= IndiceType.UnsignedInt; 
		}

		public override string ToString ()
		{
			return Name;
		}
		public static explicit operator Mesh<IndexType>(Mesh<int> mesh){
			var shortMesh = new Mesh<IndexType> ();
			shortMesh.UsageHint=mesh.UsageHint;
			shortMesh.bounds=mesh.bounds;
			shortMesh.FullPath = mesh.FullPath;
			shortMesh.Vertices = mesh.Vertices;
			shortMesh.Indices=new IndexType[mesh.Indices.Length];
			unchecked{
				for(var indice=0; indice<mesh.Indices.Length; indice++ )
					shortMesh.Indices[indice] =(IndexType)Convert.ChangeType(mesh.Indices[indice],typeof(IndexType));
			}
			return shortMesh;
		}
	}
}

