using System;
using SDL2;
using Sharp.Editor.Views;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Squid;

namespace Sharp
{
    public abstract class Window//investigate GetWindowData
    {
        private static bool quit = false;
        private static SDL.SDL_EventFilter filter = OnResize;

        public static Action onRenderFrame;
        public static Action onBeforeNextFrame;
        public static List<IntPtr> contexts = new List<IntPtr>();

        public static OrderedDictionary<uint, Window> windows = new OrderedDictionary<uint, Window>((win) => win.windowId);

        public static uint MainWindowId
        {
            get
            {
                int id = 0;
                return windows[id].windowId;
            }
        }

        public static uint TooltipWindowId
        {
            get { int id = 1; return windows[id].windowId; }
        }

        public static uint PreviewWindowId
        {
            get { int id = 2; return windows[id].windowId; }
        }

        //public static uint WhereDragStartedWindowId {
        //  get { return }
        //}
        public static uint LastCreatedWindowId
        {
            get { return windows[windows.Count - 1].windowId; }
        }

        public static uint FocusedWindowId
        {
            get { return SDL.SDL_GetWindowID(SDL.SDL_GetMouseFocus()); }
        }

        private static uint underMouseWindowId;

        public static uint UnderMouseWindowId
        {
            get
            {
                return underMouseWindowId;
            }
            set
            {
                if (windows.Contains(value) && MainEditorView.mainViews[value] != null)
                {
                    underMouseWindowId = value;
                }
            }
        }

        public static bool focusGained;

        public readonly IntPtr handle;
        public readonly uint windowId;
        //public MainEditorView mainView;  //detach it from window?
        //public bool toBeClosed = false;

        public (int width, int height) Size
        {
            set
            {
                SDL.SDL_SetWindowSize(handle, value.width, value.height);
            }
            get
            {
                SDL.SDL_GetWindowSize(handle, out int width, out int height);
                return (width, height);
            }
        }

        public (int x, int y) Position
        {
            set
            {
                SDL.SDL_SetWindowPosition(handle, value.x, value.y);
            }
            get
            {
                SDL.SDL_GetWindowPosition(handle, out int x, out int y);
                return (x, y);
            }
        }

        static Window()
        {
            SDL.SDL_AddEventWatch(filter, IntPtr.Zero);
        }

        public Window(string title, SDL.SDL_WindowFlags windowFlags, IntPtr existingWin = default(IntPtr))
        {
            if (existingWin == default(IntPtr))
                handle = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 1000, 700, windowFlags);
            else
                handle = SDL.SDL_CreateWindowFrom(existingWin);
            windowId = SDL.SDL_GetWindowID(handle);
            windows.Add(this);
            new MainEditorView(windowId);
            onRenderFrame += OnInternalRenderFrame;
        }

        public static void PollWindows()
        {
            SDL.SDL_Event sdlEvent;

            while (!quit)
            {
                while (SDL.SDL_PollEvent(out sdlEvent) != 0)
                {
                    if (windows.Contains(sdlEvent.window.windowID))
                        windows[sdlEvent.window.windowID].OnEvent(sdlEvent);
                }
                //Selection.IsSelectionDirty(System.Threading.CancellationToken.None);
                /*  if (InputHandler.mustHandleKeyboard)
                  {
                      InputHandler.ProcessKeyboard();
                      InputHandler.mustHandleKeyboard = false;
                  }*/
                InputHandler.Update();
                InputHandler.ProcessKeyboardPresses();
                InputHandler.ProcessMousePresses();

                UI.TimeElapsed = Time.deltaTime;
                UI.currentCanvas?.Update();
                //Console.WriteLine("enter");
                onRenderFrame?.Invoke();
                onBeforeNextFrame?.Invoke();
                if (UI.isDirty)
                {
                    Selection.OnSelectionDirty?.Invoke(Selection.Asset, EventArgs.Empty);
                    UI.isDirty = false;
                }
                Time.SetTime();
            }
        }

        private void OnInternalRenderFrame()
        {
            MainWindow.backendRenderer.MakeCurrent(handle, contexts[0]);
            OnRenderFrame();

            var mainView = MainEditorView.mainViews[windowId];
            if (mainView.desktop is null) return;

            mainView.Render();
            //foreach (var view in View.views[windowId])
            //  if (view.panel != null && view.panel.IsVisible)
            //    view.Render();
            MainWindow.backendRenderer.FinishCommands();
            MainWindow.backendRenderer.SwapBuffers(handle);
        }

        public abstract void OnRenderFrame();

        public void OnEvent(SDL.SDL_Event evnt)
        {
            switch (evnt.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    // if (evnt.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
                    {
                        //   quit = MainWindowId == FocusedWindowId;
                        //  windows[FocusedWindowId].Close();
                    }
                    //Console.WriteLine("1 " + (uint)'1' + " : ! " + (uint)'!');
                    InputHandler.mustHandleKeyboard = true;
                    InputHandler.ProcessKeyboard();
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    InputHandler.isMouseDragging = true;
                    HitTest(evnt.button.x, evnt.button.y);//use this to fix splitter bars
                    InputHandler.ProcessMouse();
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    focusGained = false;
                    InputHandler.isMouseDragging = false;
                    InputHandler.ProcessMouse();
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:
                    InputHandler.ProcessMouseMove();//evnt.motion.xrel instead of
                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL: InputHandler.ProcessMouseWheel(evnt.wheel.y); break;
                case SDL.SDL_EventType.SDL_WINDOWEVENT: OnWindowEvent(ref evnt.window); break;
                case SDL.SDL_EventType.SDL_QUIT: quit = true; break;
                case SDL.SDL_EventType.SDL_TEXTINPUT:
                    // char types are 8-bit in C, but 16-bit in C#, so we use a byte (8-bit) here
                    byte[] rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
                    unsafe
                    {
                        // we have a pointer to an unmanaged character array from the SDL2 lib (event.text.text),
                        // so we need to explicitly marshal into our byte array
                        //Marshal.Copy((IntPtr)evnt.text.text, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE);
                        rawBytes = new Span<byte>(evnt.text.text, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE).ToArray();
                        // the character array is null terminated, so we need to find that terminator
                    }
                    int indexOfNullTerminator = Array.IndexOf(rawBytes, (byte)0);

                    // finally, since the character array is UTF-8 encoded, get the UTF-8 string
                    string text = System.Text.Encoding.UTF8.GetString(rawBytes, 0, indexOfNullTerminator);
                    InputHandler.ProcessTextInput(text);
                    break;
            }
        }

        public static void OnWindowEvent(ref SDL.SDL_WindowEvent evt)
        {
            switch (evt.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE: if (evt.windowID == MainWindowId) quit = true; else windows[evt.windowID].Close(); break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    //case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                    MainEditorView.mainViews[evt.windowID].OnResize(evt.data1, evt.data2);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
                    MainWindow.backendRenderer.EnableScissor();
                    //UI.currentCanvas?.Update();
                    onRenderFrame?.Invoke();

                    //if (windows.Contains(evt.windowID))
                    {
                        //    windows[evt.windowID].OnInternalRenderFrame();
                        //SDL.SDL_GL_SwapWindow(windows[evt.windowID].handle);
                    }
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS: AssetsView.CheckIfDirTreeChanged(); break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    UnderMouseWindowId = evt.windowID;
                    if (MainEditorView.mainViews.TryGetValue(evt.windowID, out var mainView))
                        UI.currentCanvas = mainView.desktop;
                    SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE); break;//convert to use getglobalmousestate when no events caputred?
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    // Console.WriteLine("bu");
                    if (InputHandler.isMouseDragging)
                        SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_TRUE); break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED: break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED: if (windows.Contains(evt.windowID)) windows[evt.windowID].OnFocus(); break;
            }
        }

        private SDL.SDL_HitTestResult HitTest(int x, int y)
        {
            if (x < 5) return SDL.SDL_HitTestResult.SDL_HITTEST_RESIZE_LEFT;
            return SDL.SDL_HitTestResult.SDL_HITTEST_NORMAL;
        }

        public static void OpenView(View view, Control frame)
        {
            TabControl tabcontrol = new TabControl();
            tabcontrol.ButtonFrame.Style = "";
            tabcontrol.Dock = DockStyle.Fill;
            tabcontrol.Parent = frame;
            tabcontrol.PageFrame.Style = "frame";
            tabcontrol.Scissor = true;
            tabcontrol.PageFrame.Padding = new Margin(3, 3, 3, 3);
            tabcontrol.PageFrame.Margin = new Margin(0, -2, 0, 0);

            var tab1 = new TabPage();
            tab1.Button.Text = "test";
            tabcontrol.TabPages.Add(tab1);
            tab1.Scissor = true;
            //tab.Style = "window";
            var tab = view as TabPage;
            tabcontrol.TabPages.Add(tab);
            tabcontrol.SelectedTab = tab;
        }

        public static int OnResize(IntPtr data, IntPtr e)
        {//layers with traits like graphic/ physic/general etc.
            var evt = Marshal.PtrToStructure<SDL.SDL_Event>(e);
            switch (evt.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    focusGained = false;
                    InputHandler.isMouseDragging = false;
                    InputHandler.ProcessMouse();
                    break;

                case SDL.SDL_EventType.SDL_WINDOWEVENT: OnWindowEvent(ref evt.window); break;
            }
            return 1;
        }

        public void Hide()
        {
            SDL.SDL_HideWindow(handle);
        }

        public void Close()
        {
            SDL.SDL_DestroyWindow(handle);
            onRenderFrame -= OnInternalRenderFrame;
            windows.Remove(windowId);
        }

        public void Show()
        {
            SDL.SDL_ShowWindow(handle);
        }

        public virtual void OnFocus()
        {
        }
    }

    public class OrderedDictionary<TKey, TValue> : KeyedCollection<TKey, TValue>
    {
        private Func<TValue, TKey> _itemKey;

        public OrderedDictionary(Func<TValue, TKey> itemKey, IEqualityComparer<TKey> comparer = null, int threshold = 0)
        : base(comparer, threshold)
        {
            _itemKey = itemKey ?? throw new ArgumentNullException(nameof(itemKey));
        }

        protected override TKey GetKeyForItem(TValue item)
        {
            return _itemKey(item);
        }

        public new bool Contains(TValue value)
        {
            return Contains(GetKeyForItem(value));
        }
    }
}