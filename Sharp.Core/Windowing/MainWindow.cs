using System;
using System.Linq;
using SDL2;
using Sharp.Editor.Views;
using System.Numerics;
using SharpSL.BackendRenderers;
using Sharp.Windowing;
using Squid;
using SharpAsset.Pipeline;

namespace Sharp
{
	public class MainWindow : Window
	{
		internal static IBackendRenderer backendRenderer;
		public static Vector2 lastPos;

		public MainWindow(string title) : base(title, SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL | SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE)
		{
			SDL.SDL_SetWindowMinimumSize(handle, 500, 300);
		}

		public void Initialize(params View[] viewsToOpen)
		{
			backendRenderer.SetupGraphic();
			
			Control parent = MainEditorView.mainViews[windowId].desktop;
			var margin = new Margin(0, 50, 0, 30);
			foreach (var (id, view) in viewsToOpen.Indexed())
			{
				var splitter = new SplitContainer();
				splitter.SplitFrame1.MinSize = new Point(100, 100);
				splitter.SplitFrame2.MinSize = new Point(100, 100);
				splitter.Parent = parent;
				splitter.Margin = margin;
				splitter.Dock = DockStyle.Fill;
				splitter.SplitButton.Style = "";
				if (id == viewsToOpen.Length - 1)
				{
					splitter.SplitButton.Size = new Point(3, 0);//bug
					splitter.Orientation = Orientation.Vertical;
				}
				else
					splitter.SplitButton.Size = new Point(3, 0);

				OpenView(view, splitter.SplitFrame1);

				parent = splitter.SplitFrame2;
				margin = default;
			}
		}

		public override void OnRenderFrame()
		{
		}

		public override void OnFocus()
		{
		}
	}
}