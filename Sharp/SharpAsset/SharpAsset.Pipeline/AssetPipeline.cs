using System;
using Sharp;

namespace SharpAsset.Pipeline
{
	public abstract class Pipeline //change T to IAsset
	{
		protected Pipeline(){
			System.Attribute.GetCustomAttributes(this.GetType()); 
		}
		public abstract IAsset Import (string pathToFile);

		public abstract void Export(string pathToExport, string format);
	}
}

