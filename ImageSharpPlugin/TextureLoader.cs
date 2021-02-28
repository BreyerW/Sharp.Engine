using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.Runtime.InteropServices;
namespace ImageSharpPlugin
{
	public static class TextureLoader
	{
		public static IEnumerable<(string, int, byte[])> Import(string pathToFile)
		{
			using (var image = Image.Load<Bgra32>(pathToFile))
			{
				int[] dim = new[] { image.Width, image.Height };
				yield return ("dimensions", Marshal.SizeOf<int>(), MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref dim[0], 2)).ToArray());

				image.TryGetSinglePixelSpan(out var span);
				yield return ("data", Marshal.SizeOf<Bgra32>(), MemoryMarshal.AsBytes(span).ToArray());
			}
		}
	}
}
