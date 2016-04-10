using System;
using Gwen.Control;
using OpenTK.Input;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
	public abstract class View //sceneview, editorview, assetview, inspectorview
	{
		public static HashSet<View> views=new HashSet<View>();

		public Base panel;
		public Action makeContextCurrent;

		protected View ()
		{
			views.Add (this);
		}

		public virtual void Initialize (){
			//canvas.ShouldDrawBackground = true;
			//canvas.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
		}
		public virtual void OnContextCreated(int width, int height){}
		public virtual void Render (){
			if (panel != null) {
				var absPos =panel.LocalPosToCanvas (new System.Drawing.Point (panel.X, panel.Y));
				SceneView.backendRenderer.Scissor (panel.Margin.Left+panel.Parent.X, panel.Margin.Bottom, panel.Width,panel.Height);
				} 
			else
				SceneView.backendRenderer.Scissor (0, 0, MainEditorView.canvas.Width,MainEditorView.canvas.Height);
		}
		public virtual void OnResize (int width, int height){

			if (panel != null) {
			//	var absPos =MainEditorView.canvas.CanvasPosToLocal (new System.Drawing.Point (panel.X, panel.Y));
				//panel.SetSize (width, height);
			} else {
				MainEditorView.canvas.SetSize (width,height);
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