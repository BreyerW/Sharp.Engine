using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using OpenTK.Input;
using Sharp.Editor.Views;
using SDL2;
using System.Reflection;
using System.Linq;
using Sharp.Editor;
using Squid;
using TupleExtensions;
using System.Threading.Tasks;

namespace Sharp
{
    public static class InputHandler
    {
        private static readonly int numKeys = (int)SDL.SDL_Scancode.SDL_NUM_SCANCODES;
        private static bool pressed = false;
        private static IntPtr memAddrToKeyboard;

        internal static byte[] prevKeyState = new byte[numKeys];
        internal static byte[] curKeyState = new byte[numKeys];
        internal static List<KeyData> keyState = new List<KeyData>();

        public static (int x, int y) globalMousePosition;
        public static int wheelState;
        public static Action<KeyboardKeyEventArgs> OnKeyDown;
        public static Action<KeyboardKeyEventArgs> OnKeyUp;
        public static bool isMouseDragging = false;

        internal static bool[] prevMouseState;
        internal static bool[] curMouseState = new bool[5];

        public static Action<MouseButtonEventArgs> OnMouseDown;
        public static Action<MouseButtonEventArgs> OnMouseUp;
        public static Action<MouseMoveEventArgs> OnMouseMove;

        private static readonly uint[] mouseCodes = new uint[] { SDL.SDL_BUTTON_LMASK, SDL.SDL_BUTTON_RMASK, SDL.SDL_BUTTON_MMASK, SDL.SDL_BUTTON_X1MASK, SDL.SDL_BUTTON_X2MASK };
        private static readonly SDL.SDL_Scancode[] keyboardCodes = (SDL.SDL_Scancode[])Enum.GetValues(typeof(SDL.SDL_Scancode));
        private static List<IMenuCommand> menuCommands = new List<IMenuCommand>();//keycombinations as key
        private static SDL.SDL_Keymod modState;
        public static bool mustHandleKeyboard = false;

        static InputHandler()
        {
            Desktop.OnFocusChanged += (sender) => { if (sender is TextArea || sender is TextBox) SDL.SDL_StartTextInput(); else SDL.SDL_StopTextInput(); };
            memAddrToKeyboard = SDL.SDL_GetKeyboardState(out int _);
            var types = Assembly.GetExecutingAssembly().GetTypes();

            foreach (var type in types)
                if (type.GetInterfaces().Contains(typeof(IMenuCommand)))
                    menuCommands.Add(Activator.CreateInstance(type) as IMenuCommand);

            menuCommands.Sort((item1, item2) => (item1.keyCombination.Length >= item2.keyCombination.Length) ? 0 : 1);
        }

        public static void ProcessMouse()
        {
            EventArgs evnt = null;
            var button = SDL.SDL_GetGlobalMouseState(out globalMousePosition.x, out globalMousePosition.y);
            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            evnt = new MouseButtonEventArgs(globalMousePosition.x - winPos.x, globalMousePosition.y - winPos.y, ConvertMaskToEnum(button), true);//last param bugged
            curMouseState = new bool[5];
            foreach (var (key, val) in mouseCodes.WithIndexes())
            {
                curMouseState[key] = (button & val) == val;
            }
            if (pressed) OnMouseDown?.Invoke((MouseButtonEventArgs)evnt);
            else OnMouseUp?.Invoke((MouseButtonEventArgs)evnt);
        }

        public static void ProcessTextInput(string text)
        {
            foreach (var c in text)
            {
                keyState.Add(new KeyData() { Char = c, Pressed = true });
            }
        }

        public static void ProcessMouseMove()
        {
            if (!Window.windows.Contains(Window.UnderMouseWindowId)) return;
            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            var button = SDL.SDL_GetGlobalMouseState(out globalMousePosition.x, out globalMousePosition.y);

            if (Math.Abs(UI.MouseDelta.x) > 0 || Math.Abs(UI.MouseDelta.y) > 0)
            {
                var evnt = new MouseMoveEventArgs(globalMousePosition.x - winPos.x, globalMousePosition.y - winPos.y, UI.MouseDelta.x, UI.MouseDelta.y);
                foreach (var view in View.views[Window.UnderMouseWindowId])
                {
                    if (view.panel != null/* && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true)*/)
                    {
                        view.OnMouseMove(evnt);
                        break;
                    }
                }
                evnt = new MouseMoveEventArgs(globalMousePosition.x, globalMousePosition.y, UI.MouseDelta.x, UI.MouseDelta.y);
                OnMouseMove?.Invoke(evnt);
            }
        }

        public static void ProcessMousePresses()
        {
            //Gui.SetButtons(curMouseState);
        }

        public static void ProcessMouseWheel(int delta)
        {
            var wheelEvent = new MouseWheelEventArgs(0, 0, 0, delta);
            wheelState = -delta;
        }

        private static MouseButton ConvertMaskToEnum(uint mask)
        {
            if ((mask & SDL.SDL_BUTTON_LMASK) == SDL.SDL_BUTTON_LEFT) return MouseButton.Left;
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
            //if (Desktop.FocusedControl is TextBox || Desktop.FocusedControl is TextArea)
            //  return;

            bool combinationMet = true;
            foreach (var command in menuCommands)
            {
                combinationMet = true;
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
                if (combinationMet) { command.Execute(); return; }
            }

            foreach (var keyCode in keyboardCodes)
            {
                if (keyCode == SDL.SDL_Scancode.SDL_NUM_SCANCODES) continue;
                int key = (int)keyCode;
                if (curKeyState[key] != prevKeyState[key])
                {
                    if (curKeyState[key] is 1)
                    {
                        OnKeyDown?.Invoke(null);
                    }
                    else
                    {
                        OnKeyUp?.Invoke(null);
                    }
                }
                keyState.Add(new KeyData() { Scancode = (int)ScancodeToKeyData(keyCode), Pressed = curKeyState[key] is 1 });
                //keyState[key].Char = (char)SDL.SDL_GetKeyFromScancode(keyCode);
            }
        }

        public static void Update()
        {
            if (!Window.windows.Contains(Window.UnderMouseWindowId)) return;

            var winPos = Window.windows[Window.UnderMouseWindowId].Position;
            UI.SetMouse(globalMousePosition.x - winPos.x, globalMousePosition.y - winPos.y);
            UI.SetButtons(curMouseState);
            List<KeyData> data = new List<KeyData>();

            foreach (var key in keyState)
                data.Add(key);
            UI.SetKeyboard(data.ToArray());
            keyState.Clear();
            UI.SetMouseWheel(wheelState);
        }

        private static Keys ScancodeToKeyData(SDL.SDL_Scancode scancode)
        {
            switch (scancode)
            {
                case SDL.SDL_Scancode.SDL_SCANCODE_UNKNOWN:
                    return 0;

                case SDL.SDL_Scancode.SDL_SCANCODE_A:
                    return Keys.A;

                case SDL.SDL_Scancode.SDL_SCANCODE_B:
                    return Keys.B;

                case SDL.SDL_Scancode.SDL_SCANCODE_C:
                    return Keys.C;

                case SDL.SDL_Scancode.SDL_SCANCODE_D:
                    return Keys.D;

                case SDL.SDL_Scancode.SDL_SCANCODE_E:
                    return Keys.E;

                case SDL.SDL_Scancode.SDL_SCANCODE_F:
                    return Keys.F;

                case SDL.SDL_Scancode.SDL_SCANCODE_G:
                    return Keys.G;

                case SDL.SDL_Scancode.SDL_SCANCODE_H:
                    return Keys.H;

                case SDL.SDL_Scancode.SDL_SCANCODE_I:
                    return Keys.I;

                case SDL.SDL_Scancode.SDL_SCANCODE_J:
                    return Keys.J;

                case SDL.SDL_Scancode.SDL_SCANCODE_K:
                    return Keys.K;

                case SDL.SDL_Scancode.SDL_SCANCODE_L:
                    return Keys.L;

                case SDL.SDL_Scancode.SDL_SCANCODE_M:
                    return Keys.M;

                case SDL.SDL_Scancode.SDL_SCANCODE_N:
                    return Keys.N;

                case SDL.SDL_Scancode.SDL_SCANCODE_O:
                    return Keys.O;

                case SDL.SDL_Scancode.SDL_SCANCODE_P:
                    return Keys.P;

                case SDL.SDL_Scancode.SDL_SCANCODE_Q:
                    return Keys.Q;

                case SDL.SDL_Scancode.SDL_SCANCODE_R:
                    return Keys.R;

                case SDL.SDL_Scancode.SDL_SCANCODE_S:
                    return Keys.S;

                case SDL.SDL_Scancode.SDL_SCANCODE_T:
                    return Keys.T;

                case SDL.SDL_Scancode.SDL_SCANCODE_U:
                    return Keys.U;

                case SDL.SDL_Scancode.SDL_SCANCODE_V:
                    return Keys.V;

                case SDL.SDL_Scancode.SDL_SCANCODE_W:
                    return Keys.W;

                case SDL.SDL_Scancode.SDL_SCANCODE_X:
                    return Keys.X;

                case SDL.SDL_Scancode.SDL_SCANCODE_Y:
                    return Keys.Y;

                case SDL.SDL_Scancode.SDL_SCANCODE_Z:
                    return Keys.Z;

                case SDL.SDL_Scancode.SDL_SCANCODE_1:
                    return Keys.D1;

                case SDL.SDL_Scancode.SDL_SCANCODE_2:
                    return Keys.D2;

                case SDL.SDL_Scancode.SDL_SCANCODE_3:
                    return Keys.D3;

                case SDL.SDL_Scancode.SDL_SCANCODE_4:
                    return Keys.D4;

                case SDL.SDL_Scancode.SDL_SCANCODE_5:
                    return Keys.D5;

                case SDL.SDL_Scancode.SDL_SCANCODE_6:
                    return Keys.D6;

                case SDL.SDL_Scancode.SDL_SCANCODE_7:
                    return Keys.D7;

                case SDL.SDL_Scancode.SDL_SCANCODE_8:
                    return Keys.D8;

                case SDL.SDL_Scancode.SDL_SCANCODE_9:
                    return Keys.D9;

                case SDL.SDL_Scancode.SDL_SCANCODE_0:
                    return Keys.D0;

                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN:
                    return Keys.RETURN;

                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE:
                    return Keys.ESCAPE;

                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE:
                    return Keys.BACKSPACE;

                case SDL.SDL_Scancode.SDL_SCANCODE_TAB:
                    return Keys.TAB;

                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE:
                    return Keys.SPACE;

                case SDL.SDL_Scancode.SDL_SCANCODE_MINUS:
                    return Keys.MINUS;

                case SDL.SDL_Scancode.SDL_SCANCODE_EQUALS:
                    return Keys.EQUALS;

                case SDL.SDL_Scancode.SDL_SCANCODE_LEFTBRACKET:
                    return Keys.LEFTBRACKET;

                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET:
                    return Keys.RIGHTBRACKET;

                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSLASH:
                    return Keys.BACKSLASH;

                case SDL.SDL_Scancode.SDL_SCANCODE_NONUSHASH:
                    return Keys.BACKSLASH;

                case SDL.SDL_Scancode.SDL_SCANCODE_SEMICOLON:
                    return Keys.SEMICOLON;

                case SDL.SDL_Scancode.SDL_SCANCODE_APOSTROPHE:
                    return Keys.APOSTROPHE;

                case SDL.SDL_Scancode.SDL_SCANCODE_GRAVE:
                    return Keys.GRAVE;

                case SDL.SDL_Scancode.SDL_SCANCODE_COMMA:
                    return Keys.COMMA;

                case SDL.SDL_Scancode.SDL_SCANCODE_PERIOD:
                    return Keys.PERIOD;

                case SDL.SDL_Scancode.SDL_SCANCODE_SLASH:
                    return Keys.SLASH;

                case SDL.SDL_Scancode.SDL_SCANCODE_CAPSLOCK:
                    return Keys.CAPSLOCK;

                case SDL.SDL_Scancode.SDL_SCANCODE_F1:
                    return Keys.F1;

                case SDL.SDL_Scancode.SDL_SCANCODE_F2:
                    return Keys.F2;

                case SDL.SDL_Scancode.SDL_SCANCODE_F3:
                    return Keys.F3;

                case SDL.SDL_Scancode.SDL_SCANCODE_F4:
                    return Keys.F4;

                case SDL.SDL_Scancode.SDL_SCANCODE_F5:
                    return Keys.F5;

                case SDL.SDL_Scancode.SDL_SCANCODE_F6:
                    return Keys.F6;

                case SDL.SDL_Scancode.SDL_SCANCODE_F7:
                    return Keys.F7;

                case SDL.SDL_Scancode.SDL_SCANCODE_F8:
                    return Keys.F8;

                case SDL.SDL_Scancode.SDL_SCANCODE_F9:
                    return Keys.F9;

                case SDL.SDL_Scancode.SDL_SCANCODE_F10:
                    return Keys.F10;

                case SDL.SDL_Scancode.SDL_SCANCODE_F11:
                    return Keys.F11;

                case SDL.SDL_Scancode.SDL_SCANCODE_F12:
                    return Keys.F12;

                case SDL.SDL_Scancode.SDL_SCANCODE_PRINTSCREEN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_SCROLLLOCK:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAUSE:
                    return Keys.PAUSE;

                case SDL.SDL_Scancode.SDL_SCANCODE_INSERT:
                    return Keys.INSERT;

                case SDL.SDL_Scancode.SDL_SCANCODE_HOME:
                    return Keys.HOME;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEUP:
                    return Keys.PAGEUP;

                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE:
                    return Keys.DELETE;

                case SDL.SDL_Scancode.SDL_SCANCODE_END:
                    return Keys.END;

                case SDL.SDL_Scancode.SDL_SCANCODE_PAGEDOWN:
                    return Keys.PAGEDOWN;

                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT:
                    return Keys.RIGHT;

                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT:
                    return Keys.LEFT;

                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN:
                    return Keys.DOWN;

                case SDL.SDL_Scancode.SDL_SCANCODE_UP:
                    return Keys.UP;

                case SDL.SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR:
                    return Keys.NUMLOCK;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DIVIDE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MINUS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_ENTER:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_1:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_2:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_3:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_4:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_5:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_6:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_7:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_8:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_9:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_0:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PERIOD:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_APPLICATION:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_POWER:
                    return Keys.POWER;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_EQUALS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F13:
                    return Keys.F13;

                case SDL.SDL_Scancode.SDL_SCANCODE_F14:
                    return Keys.F14;

                case SDL.SDL_Scancode.SDL_SCANCODE_F15:
                    return Keys.F15;

                case SDL.SDL_Scancode.SDL_SCANCODE_F16:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F17:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F18:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F19:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F20:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F21:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F22:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F23:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_F24:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_EXECUTE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_HELP:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_MENU:
                    return Keys.LEFTMENU;

                case SDL.SDL_Scancode.SDL_SCANCODE_SELECT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_STOP:
                    return Keys.STOP;

                case SDL.SDL_Scancode.SDL_SCANCODE_AGAIN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_UNDO:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CUT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_COPY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_PASTE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_FIND:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_MUTE:
                    return Keys.MUTE;

                case SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEUP:
                    return Keys.VOLUMEUP;

                case SDL.SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN:
                    return Keys.VOLUMEDOWN;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_COMMA:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_EQUALSAS400:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL1:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL2:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL3:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL4:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL5:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL6:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL7:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL8:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_INTERNATIONAL9:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG1:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG2:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG3:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG4:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG5:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG6:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG7:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG8:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LANG9:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_ALTERASE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_SYSREQ:
                    return Keys.SYSRQ;

                case SDL.SDL_Scancode.SDL_SCANCODE_CANCEL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CLEAR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_PRIOR:
                    return Keys.PRIOR;

                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN2:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_SEPARATOR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_OUT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_OPER:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CLEARAGAIN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CRSEL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_EXSEL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_00:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_000:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_THOUSANDSSEPARATOR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CURRENCYUNIT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_CURRENCYSUBUNIT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_LEFTPAREN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_RIGHTPAREN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_LEFTBRACE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_RIGHTBRACE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_TAB:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_A:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_B:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_C:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_D:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_E:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_F:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_XOR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_POWER:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PERCENT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_LESS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_GREATER:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_AMPERSAND:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DBLAMPERSAND:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_VERTICALBAR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DBLVERTICALBAR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_COLON:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_HASH:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_SPACE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_AT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_EXCLAM:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMSTORE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMRECALL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMCLEAR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMADD:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMSUBTRACT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMMULTIPLY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_MEMDIVIDE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_PLUSMINUS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEAR:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_CLEARENTRY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_BINARY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_OCTAL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_DECIMAL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KP_HEXADECIMAL:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL:
                    return Keys.LEFTCONTROL;

                case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT:
                    return Keys.LEFTSHIFT;

                case SDL.SDL_Scancode.SDL_SCANCODE_LALT:
                    return Keys.LEFTALT;

                case SDL.SDL_Scancode.SDL_SCANCODE_LGUI:
                    return Keys.LEFTWINDOWS;

                case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL:
                    return Keys.RIGHTCONTROL;

                case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT:
                    return Keys.RIGHTSHIFT;

                case SDL.SDL_Scancode.SDL_SCANCODE_RALT:
                    return Keys.RIGHTALT;

                case SDL.SDL_Scancode.SDL_SCANCODE_RGUI:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_MODE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIONEXT:
                    return Keys.NEXTTRACK;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPREV:
                    return Keys.PREVTRACK;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOSTOP:
                    return Keys.MEDIASTOP;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOPLAY:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AUDIOMUTE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_MEDIASELECT:
                    return Keys.MEDIASELECT;

                case SDL.SDL_Scancode.SDL_SCANCODE_WWW:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_MAIL:
                    return Keys.MAIL;

                case SDL.SDL_Scancode.SDL_SCANCODE_CALCULATOR:
                    return Keys.CALCULATOR;

                case SDL.SDL_Scancode.SDL_SCANCODE_COMPUTER:
                    return Keys.MYCOMPUTER;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_SEARCH:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_HOME:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_BACK:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_FORWARD:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_STOP:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_REFRESH:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_AC_BOOKMARKS:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_BRIGHTNESSDOWN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_BRIGHTNESSUP:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_DISPLAYSWITCH:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KBDILLUMTOGGLE:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KBDILLUMDOWN:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_KBDILLUMUP:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_EJECT:
                    break;

                case SDL.SDL_Scancode.SDL_SCANCODE_SLEEP:
                    return Keys.SLEEP;

                case SDL.SDL_Scancode.SDL_SCANCODE_APP1:
                    return Keys.APPS;

                case SDL.SDL_Scancode.SDL_SCANCODE_APP2:
                    return Keys.APPS;
            }
            return 0;
        }

        public static void ProcessKeyboardPresses()
        {
            if (View.views.TryGetValue(SDL.SDL_GetWindowID(SDL.SDL_GetKeyboardFocus()), out var views))
                foreach (var view in views)
                    if (view.panel != null && view.panel.IsVisible)
                        view.OnKeyPressEvent(ref curKeyState);
        }
    }
}