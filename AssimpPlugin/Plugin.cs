using System;
using System.Collections.Generic;
using PluginAbstraction;

namespace AssimpPlugin
{
	public class Plugin : IPlugin
	{
		public IEnumerable<Delegate> ExportAPI()
		{
			yield return (Func<string, IEnumerable<(string, int, byte[])>>)MeshLoader.Import;
		}
		public void ImportAPI(Dictionary<string, Dictionary<string, Delegate>> plugins)
		{
			throw new NotImplementedException();
		}
		public string GetName()
		{
			return "MeshLoader";
		}

		public string GetVersion()
		{
			return "1.0";
		}


	}
}
