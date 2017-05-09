using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using Sharp.Editor.Views;
using SDL2;

namespace Sharp
{
    public static class InputHandler
    {
        private static bool pressed = false;

        internal static KeyboardState prevKeyState;
        internal static KeyboardState curKeyState;

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

        public static void ProcessMouse(uint button, bool Pressed)
        {
            EventArgs evnt = null;
            pressed = Pressed;
            SDL.SDL_GetGlobalMouseState(out int x, out int y);
            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            Gwen.Input.InputHandler.HoveredControl = input.m_Canvas.GetControlAt(x - winPos.x, y - winPos.y);
            evnt = new MouseButtonEventArgs(x - winPos.x, y - winPos.y, ConvertMaskToEnum(button), true);//last param bugged
            Gwen.Input.InputHandler.MouseFocus = Gwen.Input.InputHandler.HoveredControl;
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

        public static void ProcessKeyboard(SDL.SDL_Keycode keyCode, bool pressed)
        {
            KeyboardKeyEventArgs evnt = null;
            if (pressed)
            {
                OnKeyDown?.Invoke(evnt);
                input.ProcessKeyDown(keyCode);
            }
            else
            {
                OnKeyUp?.Invoke(evnt);
                input.ProcessKeyUp(keyCode);
            }
        }
    }
}