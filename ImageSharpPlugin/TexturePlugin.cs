using PluginAbstraction;
using System;
using System.Collections.Generic;

namespace ImageSharpPlugin
{
	public class TexturePlugin : IPlugin
	{
		public IEnumerable<Delegate> ExportAPI()
		{
			yield return (Func<string, TextureData>)TextureLoader.Import;
		}

		public string GetName()
		{
			return "TextureLoader";
		}

		public string GetVersion()
		{
			return "1.0";
		}

		public void ImportAPI(Dictionary<string, Dictionary<string, Delegate>> plugins)
		{
			throw new NotImplementedException();
		}
	}
}
