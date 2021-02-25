﻿using System.Collections.Generic;
using System;
using Sharp;
using System.Linq;
using OpenTK.Graphics;
using System.IO;

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
		public int Register(in T asset)
		{
			var name = asset.Name.ToString();
			nameToKey.Add(name);
			var i = nameToKey.IndexOf(name);
			this[i] = asset;
			recentlyLoadedAssets.Enqueue(i);
			return i;
		}

		public ref T GetAsset(string name)
		{
			return ref GetAsset(nameToKey.IndexOf(name));
		}
		public override IAsset Import(string pathToFile)
		{
			var name = Path.GetFileNameWithoutExtension(pathToFile);
			if (nameToKey.Contains(name))
				return this[nameToKey.IndexOf(name)];
			return null;
		}
	}

	public abstract class Pipeline
	{
		internal static Dictionary<Type, Pipeline> allPipelines = new Dictionary<Type, Pipeline>();
		internal static Dictionary<string, Type> extensionToTypeMapping = new Dictionary<string, Type>();
		internal static Dictionary<Type, Pipeline> assetToPipelineMapping = new Dictionary<Type, Pipeline>();

		public static void Initialize()
		{
			if (allPipelines.Any()) return;
			var type = typeof(Pipeline);
			var subclasses = type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
			foreach (var subclass in subclasses)
			{
				if (subclass.GetCustomAttributes(typeof(SupportedFilesAttribute), false).FirstOrDefault() is not SupportedFilesAttribute attr)
					continue;
				var importer = Activator.CreateInstance(subclass) as Pipeline;
				allPipelines.Add(importer.GetType().BaseType, importer);
				foreach (var format in attr.supportedFileFormats)
				{
					extensionToTypeMapping.Add(format, importer.GetType().BaseType);
				}
			}
		}

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
		public virtual IAsset Import(string pathToFile) => null;
		public abstract void Export(string pathToExport, string format);
	}
}