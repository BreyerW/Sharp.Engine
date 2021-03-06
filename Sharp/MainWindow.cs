﻿using System;
using Gtk;
using OpenTK;
using OpenTK.Input;
using Sharp;
using Sharp.Editor.Views;
using System.IO;
using System.Security.Cryptography;
using System.Collections.Generic;
using Antmicro.Migrant;
using System.Text;
//gwen to sharp.ui

public partial class MainWindow : Gtk.Window
{
	public static Vector2 lastPos;
	public static bool GLinit = false;
	public static MainEditorView mainEditorView = new MainEditorView();
	public static View focusedView;

	public MainWindow() : base(Gtk.WindowType.Toplevel)
	{
		Build();
		GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
	}
	static int oriX, oriY;
	protected bool OnIdleProcessMain()
	{

		if (!GLinit)
			return false;

		var state = Keyboard.GetState();
		foreach (var view in View.views)
			if (view.panel != null && view.panel.IsVisible)
				view.OnKeyPressEvent(ref state);
		GdkWindow?.GetOrigin(out oriX, out oriY);

		Sharp.InputHandler.ProcessMouse(oriX, oriY);
		Sharp.InputHandler.ProcessKeyboard();

		//foreach (var view in View.views)
		//if (view.canvas.IsVisible)
		if (MainEditorView.canvas.NeedsRedraw)
		{
			MainEditorView.canvas.NeedsRedraw = false;
			MainEditorView.canvas.Redraw();
			QueueDraw();
		}
		Sharp.Selection.IsSelectionDirty();
		return true;
	}
	protected void OnDeleteEvent(object sender, DeleteEventArgs a)
	{
		Application.Quit();
		a.RetVal = true;
	}

	protected void OnGlwidget1RenderFrame(object sender, EventArgs e)
	{

		OnIdleProcessMain();

		OpenTK.Graphics.OpenGL.GL.Enable(OpenTK.Graphics.OpenGL.EnableCap.ScissorTest);
		mainEditorView.Render();

		foreach (var view in View.views)
			if (view.panel != null && view.panel.IsVisible)
				view.Render();
	}
	protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
	{
		if (!GLinit)
			return false;

		foreach (var view in View.views)
			view.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
		QueueDraw();
		return base.OnConfigureEvent(evnt);
	}
	protected override void OnSizeAllocated(Gdk.Rectangle allocation)
	{
		base.OnSizeAllocated(allocation);
		if (!GLinit)
			return;

		foreach (var view in View.views)
			view.OnResize(allocation.Width, allocation.Height);
		QueueDraw();
	}
	protected override void OnGetPreferredHeightForWidth(int width, out int minimum_height, out int natural_height)
	{
		base.OnGetPreferredHeightForWidth(width, out minimum_height, out natural_height);
		OnSizeRequested(width, natural_height);
	}
	void OnSizeRequested(/*ref Requisition requisition*/int width, int height)
	{
		//base.OnSizeRequested(ref requisition);
		if (!GLinit)
			return;

		foreach (var view in View.views)
			view.OnResize(glwidget1.Allocation.Width + width, glwidget1.Allocation.Height + height);
		OnGlwidget1RenderFrame(null, null);
		QueueDraw();
	}
	//public ref int testRef(ref int param) { return ref param; }
	protected void OnGlwidget1Initialized(object sender, EventArgs e)
	{
		Console.WriteLine("init");
		//if (GLinit)
		//return;
		GLinit = true;
		MainEditorView.editorBackendRenderer = new SharpSL.BackendRenderers.OpenGL.EditorOpenGLRenderer();
		SceneView.backendRenderer = new SharpSL.BackendRenderers.OpenGL.OpenGLRenderer();
		mainEditorView.Initialize();
		mainEditorView.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);

		Sharp.InputHandler.input.Initialize(MainEditorView.canvas);


		var assets = new AssetsView();


		Sharp.InputHandler.OnMouseDown += (args) =>
		{
			Console.WriteLine("click: " + focusedView);
			if (focusedView == null)
				foreach (var view in View.views)
				{

					if (view.panel != null && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true))
					{
						view.OnMouseDown(args);
						break;
					}
				}
			else
				focusedView.OnMouseDown(args);
		};
		Sharp.InputHandler.OnMouseUp += (args) =>
		{
			if (focusedView == null)
				foreach (var view in View.views)
				{
					if (view.panel != null && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true))
					{
						view.OnMouseUp(args);
						break;
					}
				}
			else
				focusedView.OnMouseUp(args);
		};
		Sharp.InputHandler.OnMouseMove += (args) =>
		{
			if (focusedView == null)
				foreach (var view in View.views)
				{
					if (view.panel != null && view.panel.IsChild(Gwen.Input.InputHandler.HoveredControl, true))
					{
						view.OnMouseMove(args);
						break;
					}
				}
			else
				focusedView.OnMouseMove(args);
		};

		var scene = new SceneView();
		var structure = new SceneStructureView();
		var inspector = new InspectorView();

		var tab = new Gwen.Control.TabControl(mainEditorView.splitter);
		mainEditorView.splitter.OnSplitMoved += (o, args) =>
		{
			scene.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
			//assets.OnResize(glwidget1.Allocation.Width,glwidget1.Allocation.Height);
		};
		var btn = tab.AddPage("scene");
		var page = btn.Page;
		page.Margin = new Gwen.Margin(3, 3, 3, 3);
		scene.panel = btn.Page; //make GLControl for gwen

		var tab1 = new Gwen.Control.TabControl(mainEditorView.splitter);
		btn = tab1.AddPage("Assets");
		page = btn.Page;
		page.Margin = new Gwen.Margin(3, 3, 3, 3);
		page.HoverEnter += (item, args) => { Console.WriteLine("hover"); };
		assets.panel = page;

		var tab2 = new Gwen.Control.TabControl(mainEditorView.splitter);
		btn = tab2.AddPage("Structure");
		page = btn.Page;
		page.Margin = new Gwen.Margin(3, 3, 3, 3);
		structure.panel = btn.Page;

		var tab3 = new Gwen.Control.TabControl(mainEditorView.splitter);
		btn = tab3.AddPage("Inspector");
		page = btn.Page;
		page.Margin = new Gwen.Margin(3, 3, 3, 3);
		inspector.panel = btn.Page;

		scene.Initialize();
		structure.Initialize();

		inspector.Initialize();
		assets.Initialize();

		inspector.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
		scene.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
		structure.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
		assets.OnResize(glwidget1.Allocation.Width, glwidget1.Allocation.Height);

		mainEditorView.splitter.SetPanel(1, tab);
		mainEditorView.splitter.SetPanel(0, tab1);
		mainEditorView.splitter.SetPanel(2, tab2);
		mainEditorView.splitter.SetPanel(3, tab3);


		foreach (var view in View.views)
			view.OnContextCreated(glwidget1.Allocation.Width, glwidget1.Allocation.Height);
		QueueResize();
	}

	protected void OnGlwidget1ShuttingDown(object sender, EventArgs e)
	{
		throw new NotImplementedException();
	}
	[GLib.ConnectBefore]
	protected void OnGlwidget1KeyPressEvent(object o, Gtk.KeyPressEventArgs args)
	{
		//Console.WriteLine(args.Event.Key.ToString ());
	}
}
