using System;

namespace Sharp
{
	public static class Application
	{
		public static int roundingPrecision = 4;
		public static readonly string projectPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..\\..\\..\\"));//Enviroment.GetCommandLineArgs()[0]
	}
}