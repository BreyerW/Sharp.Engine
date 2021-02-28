using System;
using System.Collections.Generic;

namespace PluginAbstraction
{
	public interface IPlugin
	{
		string GetName();
		string GetVersion();
		IEnumerable<Delegate> ExportAPI();
		/// <summary>
		/// This will be called after all plugins or any are loaded or reloaded
		/// </summary>
		/// <param name="plugins"></param>
		void ImportAPI(Dictionary<string, Dictionary<string, Delegate>> plugins);
	}
}
