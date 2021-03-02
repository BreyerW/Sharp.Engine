using McMaster.NETCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginAbstraction;
using System.Runtime.CompilerServices;

namespace Sharp.Core
{
	public static class PluginManager
	{
		private static readonly Dictionary<string, Dictionary<string, Delegate>> plugins = new();

		[ModuleInitializer]
		internal static void LoadPlugins()
		{
			var loaders = new List<PluginLoader>();

			// create plugin loaders
			var pluginsDir = Path.Combine(Application.projectPath, "Plugins");
			foreach (var dir in Directory.GetDirectories(pluginsDir))
			{
				var dirName = Path.GetFileName(dir);
				var pluginDll = Path.Combine(dir, dirName + ".dll");
				if (File.Exists(pluginDll))
				{
					var loader = PluginLoader.CreateFromAssemblyFile(
						pluginDll,
						sharedTypes: new[] { typeof(IPlugin), typeof(IEnumerable<Delegate>), typeof(string), typeof(ExportAttribute) });
					loaders.Add(loader);
				}
			}

			// Create an instance of plugin types
			foreach (var loader in loaders)
			{
				var types = loader
					.LoadDefaultAssembly()
					.GetTypes();
				foreach (var pluginType in types.Where(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsAbstract))
				{
					// This assumes the implementation of IPlugin has a parameterless constructor
					IPlugin plugin = (IPlugin)Activator.CreateInstance(pluginType);
					var name = plugin.GetName();
					if (plugins.TryAdd(name, new Dictionary<string, Delegate>()) is false)
					{

					}
					foreach (var API in plugin.ExportAPI())
					{
						plugins[name].Add(API.Method.Name, API);
					}
				}
			}
		}
		public static T ImportAPI<T>(string plugin, string methodName) where T : Delegate
		{
			if (plugins.TryGetValue(plugin, out var APIs))
				if (APIs.TryGetValue(methodName, out var method))
					return method as T;

			return null;
		}
	}
}
