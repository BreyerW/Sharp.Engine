using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK;
using OpenTK.Input;
using Gwen.Control;

namespace Sharp
{
	public static class InputHandler
	{
		internal static KeyboardState prevKeyState;
		internal static KeyboardState curKeyState;

		public static Action<KeyboardKeyEventArgs> OnKeyDown; 
		public static Action<KeyboardKeyEventArgs> OnKeyUp; 

		internal static MouseState prevMouseState;
		internal static MouseState curMouseState;

		public static Action<MouseButtonEventArgs> OnMouseDown; 
		public static Action<MouseButtonEventArgs> OnMouseUp; 
		public static Action<MouseMoveEventArgs> OnMouseMove; 

		public static bool focused = false;
		public static Gwen.Input.OpenTK input=new Gwen.Input.OpenTK();

		static readonly IEnumerable<MouseButton> mouseCodes=typeof(MouseButton).GetEnumValues().Cast<MouseButton>();
		static readonly IEnumerable<Key> keyboardCodes=typeof(Key).GetEnumValues().Cast<Key>();

		public static void ProcessMouse(int oriX,int oriY){
			
			EventArgs evnt=null;
			var pressed = false;
			prevMouseState =curMouseState;
			curMouseState = Mouse.GetCursorState ();
            Gwen.Input.InputHandler.HoveredControl =input.m_Canvas.GetControlAt(curMouseState.X - oriX, curMouseState.Y - oriY);
            foreach (var mouseCode in mouseCodes)
				if (curMouseState[mouseCode]!=prevMouseState[mouseCode]){
                    evnt = new MouseButtonEventArgs(curMouseState.X - oriX, curMouseState.Y - oriY, mouseCode, true);//last param bugged
                    //evnt = new MouseButtonEventArgs (curMouseState.X, curMouseState.Y, mouseCode,true);//last param bugged
					if (curMouseState [mouseCode]) {
						pressed = true;
                        Gwen.Input.InputHandler.MouseFocus = Gwen.Input.InputHandler.HoveredControl;
                        OnMouseDown?.Invoke ((MouseButtonEventArgs)evnt);
					} else {
						OnMouseUp?.Invoke ((MouseButtonEventArgs)evnt);

					}
				}
			Vector2 delta =MainWindow.lastPos - new Vector2(curMouseState.X, curMouseState.Y);
			if (Math.Abs (delta.X) > 0 || Math.Abs (delta.Y) > 0) {
				
				evnt = new MouseMoveEventArgs (curMouseState.X-oriX, curMouseState.Y-oriY,(int)delta.X,(int)delta.Y);
				OnMouseMove?.Invoke (evnt as MouseMoveEventArgs);
			}
			input.ProcessMouseMessage (evnt,pressed);
			MainWindow.lastPos = new Vector2 (curMouseState.X, curMouseState.Y);
		}
		public static void ProcessKeyboard(){
			
			KeyboardKeyEventArgs evnt=null;
			prevKeyState = curKeyState;
			curKeyState = Keyboard.GetState();

			foreach(var keyboardCode in keyboardCodes)
				if (curKeyState[keyboardCode]!=prevKeyState[keyboardCode]){
					if (curKeyState[keyboardCode]) {
						OnKeyDown?.Invoke (evnt);

						input.ProcessKeyDown (keyboardCode);
					} else {
						OnKeyUp?.Invoke(evnt);
						input.ProcessKeyUp (keyboardCode);
					}
				}
		}
	}
}

