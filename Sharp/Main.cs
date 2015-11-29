using Gtk;

namespace Sharp
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			OpenTK.Toolkit.Init ();
			OpenTK.Graphics.GraphicsContext.ShareContexts = false;

			Application.Init ();
			MainWindow win = new MainWindow ();
			win.ShowAll ();

			Application.Run ();
		}
	}
}