using System;
using Squid;
using System.Collections.Generic;
using Sharp.Core;

namespace Sharp.Editor.Views
{
	public abstract class View : TabPage //sceneview, editorview, assetview, inspectorview
	{
		protected uint attachedToWindow;//detach view from window and just use attachedToWindow?

		public MainEditorView mainView
		{
			get
			{
				return MainEditorView.mainViews[attachedToWindow];
			}
		}

		protected View(uint attachToWindow)
		{
			attachedToWindow = attachToWindow;
			PluginManager.backendRenderer.currentWindow = attachToWindow;
			Button.AutoSize = AutoSize.Horizontal;
			NoEvents = false;
		}
	}
}