using PluginAbstraction;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace ImageSharpPlugin
{
	class FontPlugin : IPlugin
	{
		public IEnumerable<Delegate> ExportAPI()
		{
			yield return (Func<string, bool>)FontLoader.Import;
			yield return (Func<string, float, char, (float, float)>)FontLoader.LoadMetrics;
			yield return (Func<string, float, FontData>)FontLoader.LoadFontData;
			yield return (Func<string, float, char, char, Vector2>)FontLoader.LoadKerning;
			yield return (Func<string, float, char, TextureData>)FontLoader.GenerateTextureForChar;
		}

		public string GetName()
		{
			return "FontLoader";
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
