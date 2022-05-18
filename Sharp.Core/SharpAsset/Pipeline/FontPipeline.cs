using Sharp.Core;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SharpAsset.AssetPipeline
{
	[SupportedFiles(".ttf", ".otf")]
	internal class FontPipeline : Pipeline<Font>
	{
		[ModuleInitializer]
		internal static void LoadPipeline()
		{
			allPipelines.Add(typeof(FontPipeline).BaseType, instance);
			extensionToTypeMapping.Add(".ttf", typeof(FontPipeline).BaseType);
			extensionToTypeMapping.Add(".otf", typeof(FontPipeline).BaseType);
		}

		public static readonly FontPipeline instance = new();
		public static float size = 18;

		public override void ApplyAsset(in Font asset, object context)
		{
			throw new NotImplementedException();
		}

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}


		protected override ref Font ImportInternal(string pathToFileOrSystemFont)
		{
			ref var asset = ref base.ImportInternal(pathToFileOrSystemFont);
			if (Unsafe.IsNullRef(ref asset) is false) return ref asset;
			if (PluginManager.fontLoader.Import(pathToFileOrSystemFont) is false) return ref Unsafe.NullRef<Font>();
			var font = new Font(size);
			font.FullPath = pathToFileOrSystemFont;
			var data = PluginManager.fontLoader.LoadFontData(font.Name.ToString(), size);
			font.EmSize = data.emSize;
			font.Ascender = data.ascender;
			font.Descender = data.descender;
			return ref this[Register(font)];
		}
	}
	public class FontData
	{
		public ushort emSize;
		public short ascender;
		public short descender;
	}
}