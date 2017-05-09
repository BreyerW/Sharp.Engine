using System;
using SDL2;
using Sharp.Editor.Views;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sharp
{
    public abstract class Window
    {
        private static bool quit = false;
        private static SDL.SDL_EventFilter filter = OnResize;

        public static Action onRenderFrame;
        public static IntPtr context;
        public static OrderedDictionary<uint, Window> windows = new OrderedDictionary<uint, Window>((win) => win.windowId);

        public static uint MainWindowId
        {
            get { return windows[1].windowId; }
        }

        public static uint TooltipWindowId
        {
            get { return windows[2].windowId; }
        }

        public static uint PreviewWindowId
        {
            get { return windows[3].windowId; }
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
                if (windows.Contains(value))
                {
                    InputHandler.input.Initialize(windows[value].mainView.canvas);
                    underMouseWindowId = value;
                }
            }
        }

        public static bool focusGained;

        public readonly IntPtr handle;
        public readonly uint windowId;
        public MainEditorView mainView;
        //public bool toBeClosed = false;

        public (int width, int height) Size
        {
            get
            {
                SDL.SDL_GetWindowSize(handle, out int width, out int height);
                return (width, height);
            }
        }

        public (int x, int y) Position
        {
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

        public Window(string title, SDL.SDL_WindowFlags windowFlags)
        {
            handle = SDL.SDL_CreateWindow(title, SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED, 1000, 700, windowFlags);

            windowId = SDL.SDL_GetWindowID(handle);
            windows.Add(this);
            SDL.SDL_SetWindowMinimumSize(handle, 500, 300);
            View.views.Add(windowId, new HashSet<View>());
            mainView = new MainEditorView(windowId);
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
                onRenderFrame?.Invoke();
                if (Gwen.Control.Base.isDirty)
                {
                    Sharp.Selection.OnSelectionDirty?.Invoke(Sharp.Selection.Asset, EventArgs.Empty);
                    Gwen.Control.Base.isDirty = false;
                }
            }
        }

        private void OnInternalRenderFrame()
        {
            SDL.SDL_GL_MakeCurrent(handle, context);
            OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.ScissorTest);
            OnRenderFrame();
            SDL.SDL_GL_SwapWindow(handle);
        }

        public abstract void OnRenderFrame();

        public void OnEvent(SDL.SDL_Event evnt)
        {
            switch (evnt.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                case SDL.SDL_EventType.SDL_KEYUP:
                    foreach (var view in View.views[windowId])
                        if (view.panel != null && view.panel.IsVisible)
                            view.OnKeyPressEvent(evnt.key.keysym.sym);
                    Sharp.InputHandler.ProcessKeyboard(evnt.key.keysym.sym, evnt.key.type == SDL.SDL_EventType.SDL_KEYDOWN);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    InputHandler.isMouseDragging = true;
                    Sharp.InputHandler.ProcessMouse(SDL.SDL_BUTTON(evnt.button.button), true);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    focusGained = false;
                    InputHandler.isMouseDragging = false;
                    Sharp.InputHandler.ProcessMouse(SDL.SDL_BUTTON(evnt.button.button), false);
                    break;

                case SDL.SDL_EventType.SDL_MOUSEMOTION:

                    Sharp.InputHandler.ProcessMouseMove();//evnt.motion.xrel instead of

                    break;

                case SDL.SDL_EventType.SDL_MOUSEWHEEL: InputHandler.ProcessMouseWheel(evnt.wheel.y); break;
                case SDL.SDL_EventType.SDL_WINDOWEVENT: OnWindowEvent(ref evnt.window); break;
            }
        }

        public static void OnWindowEvent(ref SDL.SDL_WindowEvent evt)
        {
            switch (evt.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE: if (evt.windowID == MainWindowId) quit = true; else windows[evt.windowID].Close(); break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED:
                    if (View.views.TryGetValue(evt.windowID, out HashSet<View> views))
                        foreach (var view in views)
                            view.OnResize(evt.data1, evt.data2);
                    onRenderFrame?.Invoke();
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_TAKE_FOCUS: AssetsView.CheckIfDirTreeChanged(); break;
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    UnderMouseWindowId = evt.windowID;
                    SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_FALSE); break;//convert to use getglobalmousestate when no events caputred?
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    if (InputHandler.isMouseDragging)
                        SDL.SDL_CaptureMouse(SDL.SDL_bool.SDL_TRUE); break;
            }
        }

        public static void OpenView(View view, Window win, int index)
        {
            var tab = new Gwen.Control.TabControl(win.mainView.splitter);
            var btn = tab.AddPage(view.GetType().ToString());
            var page = btn.Page;
            page.Margin = new Gwen.Margin(3, 3, 3, 3);
            view.panel = page; //make GLControl for gwen
            view.Initialize();
            view.panel.BoundsChanged += (obj, args) => view.OnResize(view.panel.Width, view.panel.Height);
            win.mainView.splitter.SetPanel(index, tab);
        }

        public static int OnResize(IntPtr data, IntPtr e)
        {//layers with traits like graphic/ physic/general etc.
            var evt = Marshal.PtrToStructure<SDL.SDL_Event>(e);
            switch (evt.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    focusGained = false;
                    InputHandler.isMouseDragging = false;
                    Sharp.InputHandler.ProcessMouse(SDL.SDL_BUTTON(evt.button.button), false);
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