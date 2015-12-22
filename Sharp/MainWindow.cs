using System;
using Gtk;
using OpenTK;
using OpenTK.Input;
using Sharp;
using Sharp.Editor.Views;
using System.Collections.Generic;
//gwen to sharp.ui

public partial class MainWindow: Gtk.Window
{
	public static Vector2 lastPos;
	public static bool GLinit=false;
	public static MainEditorView mainEditorView=new MainEditorView(); 
	public static View focusedView;

	public MainWindow () : base (Gtk.WindowType.Toplevel)
	{
		Build ();
		GLib.Idle.Add(new GLib.IdleHandler(OnIdleProcessMain));
	}
	static int oriX, oriY;
	protected bool OnIdleProcessMain ()
	{

		if (!GLinit)
			return false;
			var state = Keyboard.GetState ();
		foreach(var view in View.views)
			if (view.canvas.IsVisible)
			view.OnKeyPressEvent (ref state);
			GdkWindow?.GetOrigin (out oriX,out oriY);

		Sharp.InputHandler.ProcessMouse (oriX, oriY);
			
		foreach (var view in View.views)
			if (view.canvas.IsVisible)
		if (view.canvas.NeedsRedraw) {
			view.canvas.NeedsRedraw = false;
			view.canvas.Redraw ();
			QueueDraw ();
			}
		return true;
	}
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected void OnGlwidget1RenderFrame (object sender, EventArgs e)
	{
		OnIdleProcessMain ();
		OpenTK.Graphics.OpenGL.GL.Enable (OpenTK.Graphics.OpenGL.EnableCap.ScissorTest);
		foreach (var view in View.views) 
			if(view.canvas.IsVisible)
			view.Render ();
	}
	protected override bool OnConfigureEvent (Gdk.EventConfigure evnt)
	{
		if (!GLinit)
			return false;
		
		foreach(var view in View.views)
			view.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);
		QueueDraw ();
		return base.OnConfigureEvent (evnt);
	}
	protected override void OnSizeAllocated (Gdk.Rectangle allocation)
	{
		base.OnSizeAllocated (allocation);
		if (!GLinit)
			return;

		foreach(var view in View.views)
			view.OnResize (allocation.Width,allocation.Height);
		QueueDraw ();
	}
	protected override void OnSizeRequested (ref Requisition requisition)
	{
		base.OnSizeRequested (ref requisition);
		if (!GLinit)
			return;
		
		foreach(var view in View.views)
			view.OnResize (glwidget1.Allocation.Width+requisition.Width,glwidget1.Allocation.Height+requisition.Height);
		QueueDraw ();
	}
	protected void OnGlwidget1Initialized (object sender, EventArgs e)
	{
		Console.WriteLine ("init");
		//if (GLinit)
		//return;
		GLinit = true;
		mainEditorView.Initialize ();
		mainEditorView.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);

		Sharp.InputHandler.input.Initialize (mainEditorView.canvas);


		var assets = new AssetsView ();
		assets.Initialize ();
		assets.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);
		Sharp.InputHandler.OnMouseDown += (args) => {
			if(focusedView==null)
			foreach(var view in View.views){
					if(view.canvas==Gwen.Input.InputHandler.HoveredControl?.GetCanvas()){
					view.OnMouseDown(args);
				break;
			}
			}
			else
				focusedView.OnMouseDown(args);
		};
		Sharp.InputHandler.OnMouseUp += (args) => {
			if(focusedView==null)
			foreach(var view in View.views){
					if(view.canvas==Gwen.Input.InputHandler.HoveredControl?.GetCanvas()){
					view.OnMouseUp(args);
					break;
				}
			}
			else
				focusedView.OnMouseUp(args);
		};
		Sharp.InputHandler.OnMouseMove += (args) => {
			if(focusedView==null)
			foreach(var view in View.views){
					if(view.canvas==Gwen.Input.InputHandler.HoveredControl?.GetCanvas()){
					view.OnMouseMove(args);
				break;
			}
			}
			else
				focusedView.OnMouseMove(args);
		};

		var scene = new SceneView ();
		scene.Initialize ();
		scene.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);

		var structure = new SceneStructureView ();
		structure.Initialize ();
		structure.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);

		var inspector = new InspectorView ();
		inspector.Initialize ();
		inspector.OnResize (glwidget1.Allocation.Width,glwidget1.Allocation.Height);


		var tab=new Gwen.Control.TabControl(mainEditorView.splitter);
		mainEditorView.splitter.OnSplitMoved+=(o,args)=>{
			scene.OnResize(glwidget1.Allocation.Width,glwidget1.Allocation.Height);
			//assets.OnResize(glwidget1.Allocation.Width,glwidget1.Allocation.Height);
		};
		var btn=tab.AddPage ("scene");
		var page = btn.Page;
		page.Margin = new Gwen.Margin (3,3,3,3);
		scene.canvas.Parent = page; //make GLControl for gwen

		var tab1=new Gwen.Control.TabControl(mainEditorView.splitter);
		btn=tab1.AddPage ("Assets");
		page = btn.Page;
		page.Margin = new Gwen.Margin (3,3,3,3);
		assets.canvas.Parent = page;

		var tab2=new Gwen.Control.TabControl(mainEditorView.splitter);
		btn=tab2.AddPage ("Structure");
		page = btn.Page;
		page.Margin = new Gwen.Margin (3,3,3,3);
		structure.canvas.Parent = page;

		var tab3=new Gwen.Control.TabControl(mainEditorView.splitter);
		btn=tab3.AddPage ("Inspector");
		page = btn.Page;
		page.Margin = new Gwen.Margin (3,3,3,3);
		inspector.canvas.Parent = page;

		mainEditorView.splitter.SetPanel(1,tab);
		mainEditorView.splitter.SetPanel(0,tab1);
		mainEditorView.splitter.SetPanel(2,tab2);
		mainEditorView.splitter.SetPanel(3,tab3);

		foreach(var view in View.views)
			view.OnContextCreated (glwidget1.Allocation.Width,glwidget1.Allocation.Height);
		QueueResize ();
	}

	protected void OnGlwidget1ShuttingDown (object sender, EventArgs e)
	{
		throw new NotImplementedException ();
	}
}
