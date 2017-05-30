﻿using System;
using Gwen.Control;
using OpenTK.Input;
using OpenTK;
using SDL2;

namespace Gwen.Input
{
    public class OpenTK
    {
        #region Properties

        public Canvas m_Canvas = null;

        private int m_MouseX = 0;
        private int m_MouseY = 0;

        private bool m_AltGr = false;

        #endregion Properties

        #region Constructors

        public OpenTK()
        {
            //window.KeyPress += KeyPress;
        }

        #endregion Constructors

        #region Methods

        public void Initialize(Canvas c)
        {
            m_Canvas = c;
        }

        /// <summary>
        /// Translates control key's OpenTK key code to GWEN's code.
        /// </summary>
        /// <param name="key">OpenTK key code.</param>
        /// <returns>GWEN key code.</returns>
        private Key TranslateKeyCode(SDL.SDL_Scancode key)
        {
            switch (key)
            {
                case SDL.SDL_Scancode.SDL_SCANCODE_BACKSPACE: return Key.Backspace;
                case SDL.SDL_Scancode.SDL_SCANCODE_RETURN: return Key.Return;
                case SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE: return Key.Escape;
                case SDL.SDL_Scancode.SDL_SCANCODE_TAB: return Key.Tab;
                case SDL.SDL_Scancode.SDL_SCANCODE_SPACE: return Key.Space;
                case SDL.SDL_Scancode.SDL_SCANCODE_UP: return Key.Up;
                case SDL.SDL_Scancode.SDL_SCANCODE_DOWN: return Key.Down;
                case SDL.SDL_Scancode.SDL_SCANCODE_LEFT: return Key.Left;
                case SDL.SDL_Scancode.SDL_SCANCODE_RIGHT: return Key.Right;
                case SDL.SDL_Scancode.SDL_SCANCODE_HOME: return Key.Home;
                case SDL.SDL_Scancode.SDL_SCANCODE_END: return Key.End;
                case SDL.SDL_Scancode.SDL_SCANCODE_DELETE: return Key.Delete;
                case SDL.SDL_Scancode.SDL_SCANCODE_LCTRL:
                    this.m_AltGr = true;
                    return Key.Control;

                case SDL.SDL_Scancode.SDL_SCANCODE_LALT: return Key.Alt;
                case SDL.SDL_Scancode.SDL_SCANCODE_LSHIFT: return Key.Shift;
                case SDL.SDL_Scancode.SDL_SCANCODE_RCTRL: return Key.Control;
                case SDL.SDL_Scancode.SDL_SCANCODE_RALT:
                    if (this.m_AltGr)
                    {
                        this.m_Canvas.Input_Key(Key.Control, false);
                    }
                    return Key.Alt;

                case SDL.SDL_Scancode.SDL_SCANCODE_RSHIFT: return Key.Shift;
            }
            return Key.Invalid;
        }

        /// <summary>
        /// Translates alphanumeric OpenTK key code to character value.
        /// </summary>
        /// <param name="key">OpenTK key code.</param>
        /// <returns>Translated character.</returns>
        private static char TranslateChar(SDL.SDL_Scancode key)
        {
            var keyCode = SDL.SDL_GetKeyFromScancode(key);
            if (key == SDL.SDL_Scancode.SDL_SCANCODE_LEFT)
                return (char)27;
            else if (key == SDL.SDL_Scancode.SDL_SCANCODE_RIGHT)
                return (char)26;
            else
            {
                var modState = SDL.SDL_GetModState();
                if (modState.HasFlag(SDL.SDL_Keymod.KMOD_LSHIFT) || modState.HasFlag(SDL.SDL_Keymod.KMOD_RSHIFT) || modState.HasFlag(SDL.SDL_Keymod.KMOD_CAPS))
                    if (keyCode >= SDL.SDL_Keycode.SDLK_AMPERSAND && keyCode <= SDL.SDL_Keycode.SDLK_UNDERSCORE)
                    {
                        return (char)(keyCode - 16);
                    }
                    else
                        return (char)(keyCode - 32);
            }
            return (char)keyCode;
        }

        public bool ProcessMouseMessage(EventArgs args, bool pressed = false)
        {
            if (null == m_Canvas) return false;

            if (args is MouseMoveEventArgs)
            {
                MouseMoveEventArgs ev = args as MouseMoveEventArgs;
                int dx = ev.X - m_MouseX;
                int dy = ev.Y - m_MouseY;

                m_MouseX = ev.X;
                m_MouseY = ev.Y;

                return m_Canvas.Input_MouseMoved(m_MouseX, m_MouseY, dx, dy);
            }

            if (args is MouseButtonEventArgs)
            {
                MouseButtonEventArgs ev = args as MouseButtonEventArgs;

                /* We can not simply cast ev.Button to an int, as 1 is middle click, not right click. */
                int ButtonID = -1; //Do not trigger event.

                if (ev.Button == MouseButton.Left)
                    ButtonID = 0;
                else if (ev.Button == MouseButton.Right)
                    ButtonID = 1;
                if (ButtonID != -1) //We only care about left and right click for now
                    return m_Canvas.Input_MouseButton(ButtonID, pressed);
            }

            if (args is MouseWheelEventArgs)
            {
                MouseWheelEventArgs ev = args as MouseWheelEventArgs;
                return m_Canvas.Input_MouseWheel(ev.Delta * 60);
            }

            return false;
        }

        public bool ProcessKeyDown(SDL.SDL_Scancode args)
        {
            // KeyboardKeyEventArgs ev = args as KeyboardKeyEventArgs;
            char ch = TranslateChar(args);
            m_Canvas.Input_Character(ch);
            //Console.WriteLine(ch);
            if (InputHandler.DoSpecialKeys(m_Canvas, ch))
                return false;

            Key iKey = TranslateKeyCode(args);
            //Console.WriteLine(iKey);
            return m_Canvas.Input_Key(iKey, true);
        }

        public bool ProcessKeyUp(SDL.SDL_Scancode args)
        {
            //char ch = TranslateChar(args);
            //m_Canvas.Input_Character(ch);
            Key iKey = TranslateKeyCode(args);

            return m_Canvas.Input_Key(iKey, false);
        }

        public void KeyPress(KeyPressEventArgs e)
        {
            m_Canvas.Input_Character(e.KeyChar);
        }

        #endregion Methods
    }
}