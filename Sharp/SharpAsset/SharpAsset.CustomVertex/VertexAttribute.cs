using System;

namespace SharpAsset
{
	[Flags]
	public enum VertexAttribute
	{
		POSITION=1<<0,
		COLOR=1<<1, 
		UV=1<<2, //return float[][] so amount of UV will be flexible
		NORMAL=1<<3,
		CUSTOM=1<<4
	}
}

