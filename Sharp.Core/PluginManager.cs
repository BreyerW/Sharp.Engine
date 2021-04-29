using McMaster.NETCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PluginAbstraction;
using System.Runtime.CompilerServices;
using Sharp.Serializer;

namespace Sharp.Core
{
	public static class PluginManager
	{
		private static readonly Dictionary<string, IPlugin> plugins = new();

		public static ISerializerPlugin serializer;
		public static IMeshLoaderPlugin meshLoader;
		public static ITextureLoaderPlugin textureLoader;
		public static IFontLoaderPlugin fontLoader;
		public static IBackendRenderer backendRenderer;

		[ModuleInitializer]
		internal static void LoadPlugins()
		{
			var loaders = new List<PluginLoader>();
			serializer = new JSONSerializer();
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
						config => config.PreferSharedTypes = true);
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
					plugins.TryAdd(name, plugin);
					switch (plugin)
					{
						case IMeshLoaderPlugin mLoader: meshLoader = mLoader; break;
						case ITextureLoaderPlugin texLoader: textureLoader = texLoader; break;
						case IFontLoaderPlugin fLoader: fontLoader = fLoader; break;
						case IBackendRenderer br: backendRenderer = br; break;
					}
				}
			}
			serializer.IsEngineObject = (obj) => obj is Component or Entity;
		}
	}
}
