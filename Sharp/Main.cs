using Gtk;
//basic acess token b2ea85e767b9919cba11ff859a099a1abb24fa72
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