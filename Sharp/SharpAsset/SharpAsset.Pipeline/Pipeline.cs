using System.Collections.Generic;
using System;
using Sharp;
using System.Linq;

namespace SharpAsset.Pipeline
{
    public abstract class Pipeline<T> : Pipeline where T : IAsset
    {
        internal static T[] assets = new T[2];
        protected static List<string> nameToKey = new List<string>();

        protected ref T this[int index]
        {
            get
            {
                if (index > assets.Length - 1)
                    Array.Resize(ref assets, assets.Length * 2);
                return ref assets[index];
            }
        }

        public abstract ref T GetAsset(int index);

        public ref T GetAsset(string name)
        {
            return ref GetAsset(nameToKey.IndexOf(name));
        }
    }

    public abstract class Pipeline
    {
        internal static Dictionary<Type, Pipeline> allPipelines = new Dictionary<Type, Pipeline>();
        internal static Dictionary<string, Type> extensionToTypeMapping = new Dictionary<string, Type>();

        public static void Initialize()
        {
            var type = typeof(Pipeline);
            var subclasses = type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
            foreach (var subclass in subclasses)
            {
                var attr = subclass.GetCustomAttributes(typeof(SupportedFilesAttribute), false).FirstOrDefault() as SupportedFilesAttribute;
                if (attr == null)
                    continue;
                var importer = Activator.CreateInstance(subclass) as Pipeline;
                allPipelines.Add(importer.GetType(), importer);
                foreach (var format in attr.supportedFileFormats)
                {
                    extensionToTypeMapping.Add(format, importer.GetType());
                }
            }
        }

        public static Pipeline GetPipeline(string extension)
        {
            return allPipelines[extensionToTypeMapping[extension]];
        }

        public static T GetPipeline<T>() where T : Pipeline
        {
            return allPipelines[typeof(T)] as T;
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