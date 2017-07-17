using System;
using Squid;
using OpenTK.Input;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
    public abstract class View //sceneview, editorview, assetview, inspectorview
    {
        protected abstract string Name { get; }

        public static Dictionary<uint, HashSet<View>> views = new Dictionary<uint, HashSet<View>>();
        public static Dictionary<uint, MainEditorView> mainViews = new Dictionary<uint, MainEditorView>();
        public Squid.Control panel;

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
            var tab = new TabPage();
            tab.Button.Text = Name;
            tab.Button.AutoSize = AutoSize.Horizontal;
            tab.NoEvents = false;
            panel = tab;
        }

        public virtual void Render()
        {
        }

        public virtual void OnResize(int width, int height)
        {
        }

        public virtual void OnMouseMove(MouseMoveEventArgs evnt)
        {
        }

        public virtual void OnMouseUp(int buttonId)
        {
        }

        public virtual void OnMouseDown(int buttonId)
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