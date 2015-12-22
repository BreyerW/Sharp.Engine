using Gtk;
using System;

namespace Sharp
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			OpenTK.Toolkit.Init ();
			OpenTK.Graphics.GraphicsContext.ShareContexts = false;
			dynamic hack="";
			hack.ToString (); //TODO: convert dirty hack to proper preload of System.Dynamic.dll
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.ShowAll ();

			Application.Run ();
		}
	}
}