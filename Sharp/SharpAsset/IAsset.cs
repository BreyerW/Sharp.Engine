using System;

namespace SharpAsset
{
	public interface IAsset
	{
		string FullPath{ get; set;}
		string Extension{ get; set;}
		string Name{ get; set;}

		//geticon ?
	}
}

