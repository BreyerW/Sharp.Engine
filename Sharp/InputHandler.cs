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
		internal static MouseState prevMouseState;
		internal static MouseState curMouseState;

		public static Action<MouseButtonEventArgs> OnMouseDown; 
		public static Action<MouseButtonEventArgs> OnMouseUp; 
		public static Action<MouseMoveEventArgs> OnMouseMove; 

		public static bool focused = false;
		public static Gwen.Input.OpenTK input=new Gwen.Input.OpenTK();

		static readonly IEnumerable<MouseButton> mouseCodes=typeof(MouseButton).GetEnumValues().Cast<MouseButton>();

		public static void ProcessMouse(int oriX,int oriY){
			
			//if (!focused)
				//return;
			EventArgs evnt=null;
			var pressed = false;
			//if(curMouseState.X > oriX + 1 && curMouseState.X < oriX + canv.Width - 1 && curMouseState.Y > oriY + 1 && curMouseState.Y < oriY + canv.Height - 1)
			//input.Initialize(canv);
			//if (!canv.Equals (input.m_Canvas))
			//	return;

			//Console.WriteLine ("buuu");
			prevMouseState =curMouseState;
			curMouseState = Mouse.GetCursorState ();//wrong
			foreach(var mouseCode in mouseCodes)
				if (curMouseState[mouseCode]!=prevMouseState[mouseCode]){
					evnt = new MouseButtonEventArgs (curMouseState.X - oriX, curMouseState.Y - oriY, mouseCode,true);//last param bugged
					if (curMouseState [mouseCode]) {
						pressed = true;

						OnMouseDown ((MouseButtonEventArgs)evnt);
					} else {
						OnMouseUp ((MouseButtonEventArgs)evnt);

					}
				}
			Vector2 delta =MainWindow.lastPos - new Vector2(curMouseState.X, curMouseState.Y);
			if (Math.Abs (delta.X) > 0 || Math.Abs (delta.Y) > 0) {
				
				evnt = new MouseMoveEventArgs (curMouseState.X-oriX, curMouseState.Y-oriY,(int)delta.X,(int)delta.Y);
				OnMouseMove (evnt as MouseMoveEventArgs);
			}
			input.ProcessMouseMessage (evnt,pressed);
			MainWindow.lastPos = new Vector2 (curMouseState.X, curMouseState.Y);
		}
	}

}

