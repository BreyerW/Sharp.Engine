using SDL3;
using Squid;
using System;

namespace Sharp.Editor
{
    internal class NativeCursor : Cursor
    {
        public SDL.SDL_SystemCursor type;

        public NativeCursor(string cursorName)
        {
            switch (cursorName)
            {
                case CursorNames.SizeWE:
                case CursorNames.VSplit: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE; break;
                case CursorNames.SizeNS:
                case CursorNames.HSplit: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE; break;
                case CursorNames.SizeNESW: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE; break;
                case CursorNames.SizeNWSE: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE; break;
                case CursorNames.Move: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE; break;
                case CursorNames.Link: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER; break;
                case CursorNames.Select: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT; break;
                case CursorNames.Reject: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED; break;
                case CursorNames.Wait: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT; break;
                default: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT; break;
            }
        }

        public override void Draw(int x, int y)
        {
            var cursor = SDL.SDL_CreateSystemCursor(type);
            //var surface = SDL.SDL_LoadBMP(@"B:\Sharp.Engine3\Sharp\SharpSL\SharpSL.BackendRenderers\Content\Cursors\aero_unavail_xl-4.bmp");
            //cursor = SDL.SDL_CreateColorCursor(surface, 0, 0);
            SDL.SDL_SetCursor(cursor);
        }
    }
}