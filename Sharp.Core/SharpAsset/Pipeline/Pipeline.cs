using System.Collections.Generic;
using System;
using Sharp;
using System.Linq;

namespace SharpAsset.Pipeline
{
	public abstract class Pipeline<T> : Pipeline where T : IAsset
	{
		private static T[] assets = new T[2];
		internal static List<string> nameToKey = new List<string>();
		public static Queue<int> recentlyLoadedAssets = new Queue<int>();
		private protected Pipeline()
		{
			assetToPipelineMapping.Add(typeof(T), this);
		}
		protected ref T this[int index]
		{
			get
			{
				if (index > assets.Length - 1)
					Array.Resize(ref assets, assets.Length * 2);
				return ref assets[index];
			}
		}

		public ref T GetAsset(int index)
		{
			return ref this[index];
		}
		public int Register(ref T asset)
		{
			nameToKey.Add(asset.Name);
			var i = nameToKey.IndexOf(asset.Name);
			this[i] = asset;
			recentlyLoadedAssets.Enqueue(i);
			return i;
		}

		public ref T GetAsset(string name)
		{
			return ref GetAsset(nameToKey.IndexOf(name));
		}
	}

	public abstract class Pipeline
	{
		internal static Dictionary<Type, Pipeline> allPipelines = new Dictionary<Type, Pipeline>();
		internal static Dictionary<string, Type> extensionToTypeMapping = new Dictionary<string, Type>();
		internal static Dictionary<Type, Pipeline> assetToPipelineMapping = new Dictionary<Type, Pipeline>();

		public static void Initialize()
		{
			var type = typeof(Pipeline);
			var subclasses = type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
			foreach (var subclass in subclasses)
			{
				if (!(subclass.GetCustomAttributes(typeof(SupportedFilesAttribute), false).FirstOrDefault() is SupportedFilesAttribute attr))
					continue;
				var importer = Activator.CreateInstance(subclass) as Pipeline;
				allPipelines.Add(importer.GetType().BaseType, importer);
				foreach (var format in attr.supportedFileFormats)
				{
					extensionToTypeMapping.Add(format, importer.GetType().BaseType);
				}
			}
		}

		public static Pipeline GetPipeline(string extension)
		{
			return allPipelines[extensionToTypeMapping[extension]];
		}

		public static Pipeline<T> Get<T>() where T : IAsset
		{
			return allPipelines[typeof(Pipeline<T>)] as Pipeline<T>;
		}

		public static bool IsExtensionSupported(string extension)
		{
			return extensionToTypeMapping.ContainsKey(extension);
		}

		public static string[] GetSupportedExtensions()
		{
			return extensionToTypeMapping.Keys.ToArray();
		}

		public abstract IAsset Import(string pathToFile);

		public abstract void Export(string pathToExport, string format);
	}
}