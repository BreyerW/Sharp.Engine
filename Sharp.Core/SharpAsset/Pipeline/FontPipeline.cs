using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace SharpAsset.Pipeline
{
	[SupportedFiles(".ttf", ".otf")]
	internal class FontPipeline : Pipeline<Font>
	{

		public override void Export(string pathToExport, string format)
		{
			throw new NotImplementedException();
		}

		public override IAsset Import(string pathToFile)
		{

			var name = Path.GetFileNameWithoutExtension(pathToFile);
			if (nameToKey.Contains(name))
				return this[nameToKey.IndexOf(name)];
			var font = new Font() { metrics = new Dictionary<uint, (Texture tex, float bearing, float advance)>() };
			font.FullPath = pathToFile;
			return this[Register(font)];
		}
	}
}