using System;
using Squid;
using SDL2;

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
                case CursorNames.VSplit: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEWE; break;
                case CursorNames.SizeNS:
                case CursorNames.HSplit: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENS; break;
                case CursorNames.SizeNESW: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENESW; break;
                case CursorNames.SizeNWSE: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZENWSE; break;
                case CursorNames.Move: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_SIZEALL; break;
                case CursorNames.Link: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_HAND; break;
                case CursorNames.Select: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_IBEAM; break;
                case CursorNames.Reject: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_NO; break;
                case CursorNames.Wait: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT; break;
                default: type = SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_ARROW; break;
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