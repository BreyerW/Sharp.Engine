using Sharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SharpAsset.AssetPipeline
{
	public abstract class Pipeline<T> : Pipeline where T : IAsset
	{
		private static T[] assets = new T[2];
		internal static List<string> nameToKey = new List<string>();
		public static Queue<int> recentlyLoadedAssets = new Queue<int>();

		private protected Pipeline()
		{
			assetToPipelineMapping.TryAdd(typeof(T), this);
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
		public static ref T GetAsset(int index)
		{
			return ref assets[index];
		}
		public static ref T GetAsset(string name)
		{
			return ref GetAsset(nameToKey.IndexOf(name));
		}
		public int Register(in T asset)
		{
			var name = asset.Name.ToString();
			if (nameToKey.Contains(name) is false)
				nameToKey.Add(name);
			var i = nameToKey.IndexOf(name);
			if (i > assets.Length - 1)
				Array.Resize(ref assets, assets.Length * 2);
			assets[i] = asset;
			//if (recentlyLoadedAssets.Count is 0)
			//	Coroutine.Start(GenerateId());
			recentlyLoadedAssets.Enqueue(i);

			return i;
		}
		public abstract void ApplyAsset(in T asset, object context);
		public virtual void GenerateGraphicDeviceId() { }

		protected virtual ref T ImportInternal(string pathToFile)
		{
			var name = Path.GetFileNameWithoutExtension(pathToFile);
			if (nameToKey.Contains(name))
				return ref this[nameToKey.IndexOf(name)];
			return ref Unsafe.NullRef<T>();
		}
		public static ref T Import(string pathToFile)
		{
			return ref (assetToPipelineMapping[typeof(T)] as Pipeline<T>).ImportInternal(pathToFile);
		}
		public sealed override IAsset ImportIAsset(string pathToFile)
		{
			return ImportInternal(pathToFile);
		}
		public sealed override void ApplyIAsset(IAsset asset, object context)
		{
			ApplyAsset(GetAsset(asset.Name.ToString()), context);
		}
	}

	public abstract class Pipeline
	{
		internal static Dictionary<Type, Pipeline> allPipelines = new Dictionary<Type, Pipeline>();
		internal static Dictionary<string, Type> extensionToTypeMapping = new Dictionary<string, Type>();
		internal static Dictionary<Type, Pipeline> assetToPipelineMapping = new Dictionary<Type, Pipeline>();

		public static Pipeline Get(string extension)
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
		public abstract IAsset ImportIAsset(string pathToFile);
		public abstract void ApplyIAsset(IAsset asset, object context);
		public abstract void Export(string pathToExport, string format);
	}
}