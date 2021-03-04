using System;

namespace PluginAbstraction
{
	public class SerializedAttribute : Attribute
	{
		public bool IsReference;
		public SerializedAttribute(bool IsReference=true)
		{
			this.IsReference = IsReference;
		}
	}
}
