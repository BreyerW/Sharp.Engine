using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK;
using OpenTK.Input;
using Sharp.Editor.Views;
using SDL2;
using System.Reflection;
using System.Linq;
using Sharp.Editor;

namespace Sharp
{
    public static class InputHandler
    {
        private static readonly int numKeys = (int)SDL.SDL_Scancode.SDL_NUM_SCANCODES;
        private static bool pressed = false;
        private static IntPtr memAddrToKeyboard;

        internal static byte[] prevKeyState = new byte[numKeys];
        internal static byte[] curKeyState = new byte[numKeys];

        public static Action<KeyboardKeyEventArgs> OnKeyDown;
        public static Action<KeyboardKeyEventArgs> OnKeyUp;
        public static bool isMouseDragging = false;

        internal static uint prevMouseState;
        internal static uint curMouseState;

        public static Action<MouseButtonEventArgs> OnMouseDown;
        public static Action<MouseButtonEventArgs> OnMouseUp;
        public static Action<MouseMoveEventArgs> OnMouseMove;
        public static Gwen.Input.OpenTK input = new Gwen.Input.OpenTK();

        private static readonly uint[] mouseCodes = new uint[] { SDL.SDL_BUTTON_LMASK, SDL.SDL_BUTTON_MMASK, SDL.SDL_BUTTON_RMASK, SDL.SDL_BUTTON_X1MASK, SDL.SDL_BUTTON_X2MASK };
        private static readonly SDL.SDL_Scancode[] keyboardCodes = (SDL.SDL_Scancode[])Enum.GetValues(typeof(SDL.SDL_Scancode));
        private static List<IMenuCommand> menuCommands = new List<IMenuCommand>();//keycombinations as key
        private static SDL.SDL_Keymod modState;

        static InputHandler()
        {
            memAddrToKeyboard = SDL.SDL_GetKeyboardState(out int _);
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
                if (type.GetInterfaces().Contains(typeof(IMenuCommand)))
                    menuCommands.Add(Activator.CreateInstance(type) as IMenuCommand);

            menuCommands.Sort((item1, item2) => (item1.keyCombination.Length >= item2.keyCombination.Length) ? 0 : 1);
        }

        public static void ProcessMouse(uint button, bool Pressed)
        {
            EventArgs evnt = null;
            pressed = Pressed;
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            Gwen.Input.InputHandler.HoveredControl = input.m_Canvas.GetControlAt(x - winPos.x, y - winPos.y);
            evnt = new MouseButtonEventArgs(x - winPos.x, y - winPos.y, ConvertMaskToEnum(button), true);//last param bugged
            Gwen.Input.InputHandler.MouseFocus = Gwen.Input.InputHandler.HoveredControl;
            Gwen.Input.InputHandler.KeyboardFocus = Gwen.Input.InputHandler.HoveredControl as Gwen.Control.TextBox;
            foreach (var view in View.views[Window.UnderMouseWindowId])
            {
                if (view.panel != null && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true))
                {
                    if (pressed)
                        view.OnMouseDown((MouseButtonEventArgs)evnt);
                    else
                        view.OnMouseUp((MouseButtonEventArgs)evnt);
                    break;
                }
            }
            if (pressed) OnMouseDown?.Invoke((MouseButtonEventArgs)evnt);
            else OnMouseUp?.Invoke((MouseButtonEventArgs)evnt);

            input.ProcessMouseMessage(evnt, pressed);
        }

        public static void ProcessMouseMove()
        {
            if (!Window.windows.Contains(Window.UnderMouseWindowId)) return;
            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            Gwen.Input.InputHandler.HoveredControl = input.m_Canvas.GetControlAt(x - winPos.x, y - winPos.y); //change ori to current mouseover window
            Vector2 delta = MainWindow.lastPos - new Vector2(x, y);

            if (Math.Abs(delta.X) > 0 || Math.Abs(delta.Y) > 0)
            {
                var evnt = new MouseMoveEventArgs(x - winPos.x, y - winPos.y, (int)delta.X, (int)delta.Y);
                foreach (var view in View.views[Window.UnderMouseWindowId])
                {
                    if (view.panel != null && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true))
                    {
                        view.OnMouseMove(evnt);
                        break;
                    }
                }
                input.ProcessMouseMessage(evnt, pressed);
                evnt = new MouseMoveEventArgs(x, y, (int)delta.X, (int)delta.Y);
                OnMouseMove?.Invoke(evnt);
            }
            MainWindow.lastPos = new Vector2(x, y);
        }

        public static void ProcessMouseWheel(int delta)
        {
            var wheelEvent = new MouseWheelEventArgs(0, 0, 0, delta);
            input.ProcessMouseMessage(wheelEvent);
        }

        private static MouseButton ConvertMaskToEnum(uint mask)
        {
            if (mask == SDL.SDL_BUTTON_LMASK) return MouseButton.Left;
            else if (mask == SDL.SDL_BUTTON_MMASK) return MouseButton.Middle;
            else if (mask == SDL.SDL_BUTTON_RMASK) return MouseButton.Right;
            else if (mask == SDL.SDL_BUTTON_X1MASK) return MouseButton.Button1;
            else if (mask == SDL.SDL_BUTTON_X2MASK) return MouseButton.Button2;
            else return MouseButton.LastButton;
        }

        public static void ProcessKeyboard()
        {
            modState = SDL.SDL_GetModState();
            Unsafe.CopyBlock(ref prevKeyState[0], ref curKeyState[0], (uint)numKeys);
            Marshal.Copy(memAddrToKeyboard, curKeyState, 0, numKeys);

            foreach (var keyCode in keyboardCodes)
            {
                if (keyCode == SDL.SDL_Scancode.SDL_NUM_SCANCODES) continue;
                int key = (int)keyCode;
                if (curKeyState[key] != prevKeyState[key])
                {
                    if (pressed = curKeyState[key] is 1)
                    {
                        OnKeyDown?.Invoke(null);
                        input.ProcessKeyDown(keyCode);
                    }
                    else
                    {
                        OnKeyUp?.Invoke(null);
                        input.ProcessKeyUp(keyCode);
                    }
                }
            }
            foreach (var command in menuCommands)
            {
                var combinationMet = true;
                foreach (var key in command.keyCombination)
                {
                    switch (key)
                    {
                        case "CTRL": combinationMet = modState.HasFlag(SDL.SDL_Keymod.KMOD_LCTRL); break;
                        case "SHIFT": combinationMet = modState.HasFlag(SDL.SDL_Keymod.KMOD_LSHIFT) || modState.HasFlag(SDL.SDL_Keymod.KMOD_RSHIFT); break;
                        default:
                            var keyCode = (int)SDL.SDL_GetScancodeFromKey((SDL.SDL_Keycode)key.ToCharArray()[0]);
                            combinationMet = curKeyState[keyCode] is 1; break;
                    }
                    if (!combinationMet) break;
                }
                if (combinationMet) { command.Execute(); break; }
            }
        }

        public static void ProcessKeyboardPresses()
        {
            if (!(Gwen.Input.InputHandler.KeyboardFocus is null)) return;
            if (View.views.TryGetValue(SDL.SDL_GetWindowID(SDL.SDL_GetKeyboardFocus()), out var views))
                foreach (var view in views)
                    if (view.panel != null && view.panel.IsVisible)
                        view.OnKeyPressEvent(ref curKeyState);
        }
    }
}