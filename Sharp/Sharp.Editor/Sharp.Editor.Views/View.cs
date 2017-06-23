using System;
using Gwen.Control;
using OpenTK.Input;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
    public abstract class View //sceneview, editorview, assetview, inspectorview
    {
        public static Dictionary<uint, HashSet<View>> views = new Dictionary<uint, HashSet<View>>();
        public static Dictionary<uint, MainEditorView> mainViews = new Dictionary<uint, MainEditorView>();
        public Base panel;

        protected uint attachedToWindow;//detach view from window and just use attachedToWindow?

        public MainEditorView mainView
        {
            get
            {
                return mainViews[attachedToWindow];
            }
        }

        protected View(uint attachToWindow)
        {
            if (this is MainEditorView mainView)
                mainViews.Add(attachToWindow, mainView);
            else
                views[attachToWindow].Add(this);
            attachedToWindow = attachToWindow;
            InputHandler.OnMouseMove += OnGlobalMouseMove;
            InputHandler.OnMouseUp += OnGlobalMouseUp;
            InputHandler.OnMouseDown += OnGlobalMouseDown;
        }

        public virtual void Initialize()
        {
            //canvas.ShouldDrawBackground = true;
            //canvas.BackgroundColor = Color.FromArgb(255, 150, 170, 170);
        }

        public virtual void Render()
        {
            if (panel != null)
            {
                var absPos = panel.LocalPosToCanvas(new System.Drawing.Point(panel.X, panel.Y));
                MainWindow.backendRenderer.Viewport(panel.Margin.Left + panel.Parent.X, panel.Margin.Bottom, panel.Width, panel.Height);
            }
            else
                MainWindow.backendRenderer.Viewport(0, 0, mainView.canvas.Width, mainView.canvas.Height);
        }

        public virtual void OnResize(int width, int height)
        {
        }

        public virtual void OnMouseMove(MouseMoveEventArgs evnt)
        {
        }

        public virtual void OnMouseUp(MouseButtonEventArgs evnt)
        {
        }

        public virtual void OnMouseDown(MouseButtonEventArgs evnt)
        {
        }

        public virtual void OnGlobalMouseMove(MouseMoveEventArgs evnt/*View startView, View overView*/)
        {
        }

        public virtual void OnGlobalMouseUp(MouseButtonEventArgs evnt)
        {
        }

        public virtual void OnGlobalMouseDown(MouseButtonEventArgs evnt)
        {
        }

        public virtual void OnKeyPressEvent(ref byte[] keyboardState)
        {
        }
    }
}