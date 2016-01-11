using System;
using Gwen.Control;
using OpenTK.Input;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
	public abstract class View //sceneview, editorview, assetview, inspectorview
	{
		public static HashSet<View> views=new HashSet<View>();

		public Canvas canvas;
		public Action makeContextCurrent;

		protected View ()
		{
			views.Add (this);
		}

		public virtual void Initialize (){
			
			canvas = new Canvas(MainEditorView.skin);
			//canvas.ShouldDrawBackground = true;
			//canvas.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
		}
		public virtual void OnContextCreated(int width, int height){}
		public virtual void Render (){
			if (canvas.Parent != null) {
				var absPos = canvas.CanvasPosToLocal (new System.Drawing.Point (canvas.X, canvas.Y));
				SceneView.backendRenderer.Scissor (-absPos.X, canvas.Parent.Margin.Bottom, canvas.Parent.Width,canvas.Height);
			} 
			else
				SceneView.backendRenderer.Scissor (0, 0, canvas.Width, canvas.Height);
		}
		public virtual void OnResize (int width, int height){

			if (canvas.Parent != null) {
				var absPos = canvas.CanvasPosToLocal (new System.Drawing.Point (canvas.X, canvas.Y));
				canvas.SetSize (canvas.Parent.Width, canvas.Parent.Height);
			} else {
				
				canvas.SetSize (width,height);
			}
			MainEditorView.renderer.Resize (width,height);

		}
		public virtual void OnMouseMove(MouseMoveEventArgs evnt){
		}
		public virtual void OnMouseUp(MouseButtonEventArgs evnt){
		}
		public virtual void OnMouseDown(MouseButtonEventArgs evnt){
		}
		public virtual void OnKeyPressEvent (ref KeyboardState evnt){
		}
	}
}

