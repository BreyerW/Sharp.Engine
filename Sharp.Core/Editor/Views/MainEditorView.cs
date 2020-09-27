using SharpSL;
using Squid;
using System.Collections.Generic;
using System.Numerics;
using System;
using SharpSL.BackendRenderers;

namespace Sharp.Editor.Views
{
	public class MainEditorView
	{
		public static Dictionary<uint, MainEditorView> mainViews = new Dictionary<uint, MainEditorView>();
		public static MainEditorView currentMainView;
		public Desktop desktop;
		public EditorCamera camera = new EditorCamera();

		//public SplitContainer splitter = new SplitContainer();
		public bool needRedraw = false;

		public static IEditorBackendRenderer editorBackendRenderer;

		static MainEditorView()
		{
			Desktop.CursorSet.Add(CursorNames.Default, new NativeCursor(CursorNames.Default));
			Desktop.CursorSet.Add(CursorNames.Link, new NativeCursor(CursorNames.Link));
			Desktop.CursorSet.Add(CursorNames.Move, new NativeCursor(CursorNames.Move));
			Desktop.CursorSet.Add(CursorNames.HSplit, new NativeCursor(CursorNames.HSplit));
			Desktop.CursorSet.Add(CursorNames.VSplit, new NativeCursor(CursorNames.VSplit));
			Desktop.CursorSet.Add(CursorNames.SizeNS, new NativeCursor(CursorNames.SizeNS));
			Desktop.CursorSet.Add(CursorNames.SizeWE, new NativeCursor(CursorNames.SizeWE));
			Desktop.CursorSet.Add(CursorNames.SizeNWSE, new NativeCursor(CursorNames.SizeNWSE));
			Desktop.CursorSet.Add(CursorNames.SizeNESW, new NativeCursor(CursorNames.SizeNESW));
			Desktop.CursorSet.Add(CursorNames.Select, new NativeCursor(CursorNames.Select));
			Desktop.CursorSet.Add(CursorNames.Reject, new NativeCursor(CursorNames.Reject));
			Desktop.CursorSet.Add(CursorNames.Wait, new NativeCursor(CursorNames.Wait));
		}

		/*
         *
        // XPM

        private static string[] arrow = new string[] {
  // width height num_colors chars_per_pixel
  "    32    32        3            1",
  // colors
  "X c #000000",
  ". c #ffffff",
  "  c None",
  // pixels
  "X                               ",
  "XX                              ",
  "X.X                             ",
  "X..X                            ",
  "X...X                           ",
  "X....X                          ",
  "X.....X                         ",
  "X......X                        ",
  "X.......X                       ",
  "X........X                      ",
  "X.....XXXXX                     ",
  "X..X..X                         ",
  "X.X X..X                        ",
  "XX  X..X                        ",
  "X    X..X                       ",
  "     X..X                       ",
  "      X..X                      ",
  "      X..X                      ",
  "       XX                       ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "                                ",
  "0,0"
        };
        private (IntPtr, IntPtr) ConvertToCursor()
        {
            int i, row, col;
            //byte[] data=new byte[4 * 32];
            //byte[] mask= new byte[4 * 32];
            int hot_x, hot_y;
            var image = arrow;
            var dataPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(4 * 32);
            var maskPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(4 * 32);
            unsafe
            {
                var data = new Span<byte>(dataPtr.ToPointer(), 4 * 32);
                var mask = new Span<byte>(maskPtr.ToPointer(), 4 * 32);

                i = -1;
                for (row = 0; row < 32; ++row)
                {
                    for (col = 0; col < 32; ++col)
                    {
                        if (!((col % 8) is 0))
                        {
                            data[i] <<= 1;
                            mask[i] <<= 1;
                        }
                        else
                        {
                            ++i;
                            data[i] = mask[i] = 0;
                        }
                        switch (image[4 + row][col])
                        {
                            case 'X':
                                data[i] |= 0x01;
                                mask[i] |= 0x01;
                                break;

                            case '.':
                                mask[i] |= 0x01;
                                break;

                            case ' ':
                                break;
                        }
                    }
                }
            }
            return (dataPtr, maskPtr);
        }*/

		public MainEditorView(uint attachToWindow)
		{
			if (currentMainView is null)
				currentMainView = this;
			mainViews.Add(attachToWindow, this);
			desktop = new Desktop();
			var winPos = Window.windows[attachToWindow].Position;
			desktop.screenPos = new Point(winPos.x, winPos.y);
			desktop.ShowCursor = true;
			desktop.Skin = Squid.UI.GenerateStandardSkin();
			desktop.Name = "desktop " + attachToWindow;
			Squid.UI.Renderer = new UIRenderer();
		}

		//split.Depth = 1;//depth conflict when two controls overlap with same parent - fix it
		public int nextUpdate;

		public void Render()
		{
			currentMainView = this;
			MainWindow.backendRenderer.Viewport(0, 0, desktop.Size.x, desktop.Size.y);
			//MainWindow.backendRenderer.Clip(0, 0, desktop.Size.x, desktop.Size.y);
			MainWindow.backendRenderer.ClearBuffer();
			MainWindow.backendRenderer.ClearColor();
			//desktop.NoEvents = Window.UnderMouseWindowId == attachedToWindow;
			desktop.Draw();
		}

		public void OnInternalUpdate()
		{
			desktop.Update();//TODO: fix mouse event and keyboards on other windows
		}

		public void OnResize(int width, int height)
		{
			camera.SetOrthoMatrix(width, height);
			desktop.Size = new Point(width, height);
		}
	}
}