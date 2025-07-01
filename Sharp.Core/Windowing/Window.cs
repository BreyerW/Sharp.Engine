using Assimp;
using PluginAbstraction;
using SDL3;
using Sharp.Core;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using SharpAsset;
using SharpAsset.AssetPipeline;
using SharpSL;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Sharp
{
	public abstract class Window//investigate GetWindowData
	{
		private static Window mainWindow;
		private static Window tooltipWindow;
		private static Window previewWindow;

		private static bool quit = false;
		private unsafe static SDL.SDL_EventFilter filter = OnResize;

		public static Action onRenderFrame;
		public static Action onBeforeNextFrame;
		public static List<IntPtr> contexts = new List<IntPtr>();

		public static Dictionary<uint, Window> windows = new();

		public static uint MainWindowId
		{
			get => mainWindow.windowId;
		}

		public static uint TooltipWindowId
		{
			get => tooltipWindow.windowId;
		}

		public static uint PreviewWindowId
		{
			get => previewWindow.windowId;
		}

		//public static uint WhereDragStartedWindowId {
		//  get { return }
		//}
		/*public static uint LastCreatedWindowId
		{
			get { return windows[windows.Count - 1].windowId; }
		}*/

		public static uint FocusedWindowId
		{
			get { return SDL.SDL_GetWindowID(SDL.SDL_GetMouseFocus()); }
		}

		private static uint underMouseWindowId;

		public static uint UnderMouseWindowId
		{
			get
			{
				return underMouseWindowId;
			}
			set
			{
				if (windows.ContainsKey(value) && MainEditorView.mainViews[value] != null)
				{
					underMouseWindowId = value;
				}
			}
		}

		public static bool focusGained;

		public readonly IntPtr handle;
		public readonly uint windowId;
		//public MainEditorView mainView;  //detach it from window?
		//public bool toBeClosed = false;

		public (int width, int height) Size
		{
			set
			{
				SDL.SDL_SetWindowSize(handle, value.width, value.height);
			}
			get
			{
				SDL.SDL_GetWindowSize(handle, out int width, out int height);
				return (width, height);
			}
		}

		public (int x, int y) Position
		{
			set
			{
				SDL.SDL_SetWindowPosition(handle, value.x, value.y);
			}
			get
			{
				SDL.SDL_GetWindowPosition(handle, out int x, out int y);
				return (x, y);
			}
		}

		static Window()
		{
			SDL.SDL_AddEventWatch(filter, IntPtr.Zero);
		}


		public Window(string title, SDL.SDL_WindowFlags windowFlags, IntPtr existingWin = default)
		{
			var props = SDL.SDL_CreateProperties();
			SDL.SDL_SetStringProperty(props, SDL.SDL_PROP_WINDOW_CREATE_TITLE_STRING, title);
			//SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_X_NUMBER, x);
			//SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_Y_NUMBER, y);
			SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, 1000);
			SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, 700);
			// For window flags you should use separate window creation properties,
			// but for easier migration from SDL2 you can use the following:
			SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_FLAGS_NUMBER, (long)windowFlags);
			if (existingWin == default)
				SDL.SDL_SetPointerProperty(props, SDL.SDL_PROP_WINDOW_CREATE_PARENT_POINTER, existingWin);
			//else
			handle = SDL.SDL_CreateWindowWithProperties(props);
			windowId = SDL.SDL_GetWindowID(handle);
			windows.Add(windowId, this);
			if (windows.Count is 1)
				mainWindow = this;
			else if (windows.Count is 2)
				tooltipWindow = this;
			else if (windows.Count is 3)
				previewWindow = this;
			new MainEditorView(windowId);
			onRenderFrame += OnInternalRenderFrame;
		}

		public static void PollWindows()
		{
			SDL.SDL_Event sdlEvent;
			while (!quit)
			{

				Coroutine.AdvanceInstructions<WaitForStartOfFrame>();

				while (SDL.SDL_PollEvent(out sdlEvent))
				{
					if (windows.ContainsKey(sdlEvent.window.windowID))
						windows[sdlEvent.window.windowID].OnEvent(sdlEvent);
				}

				PluginManager.backendRenderer.EnableState(RenderState.ScissorTest);

				//Selection.IsSelectionDirty(System.Threading.CancellationToken.None);
				/*  if (InputHandler.mustHandleKeyboard)
                  {
                      InputHandler.ProcessKeyboard();
                      InputHandler.mustHandleKeyboard = false;
                  }*/
				InputHandler.Update();
				InputHandler.ProcessKeyboardPresses();
				InputHandler.ProcessMousePresses();

				UI.TimeElapsed = Time.deltaTime;
				//UI.currentCanvas?.Update();//TODO: change it so that during dragging it will update both source and hovered window
				var copy = new MainEditorView[MainEditorView.mainViews.Values.Count];
				//MainEditorView.mainViews.Values.CopyTo(copy, 0);
				foreach (var mainV in MainEditorView.mainViews.Values)
					mainV.OnInternalUpdate();


				onRenderFrame?.Invoke();
				if (UI.isDirty)
				{
					//Selection.OnSelectionDirty?.Invoke(Selection.Asset);
					UI.isDirty = false;
				}
				Coroutine.AdvanceInstructions<WaitForEndOfFrame>();
				//foreach(var pipeline in Pipeline.allPipelines.Values)
				//	while(pipeline.recentlyLoadedAssets.TryDequeue(out var i)) //TODO

				Time.SetTime();
				Coroutine.AdvanceInstructions<WaitForSeconds>();
				//IdReferenceResolver._objectsToId.Clear();
				//IdReferenceResolver._idToObjects.Clear();
				/*foreach (var removed in Root.removedEntities)
				{
					PluginManager.serializer.objToIdMapping.Remove(removed);
				}*/
				Root.removedEntities.Clear();
				Root.addedEntities.Clear();
			}
		}
		private void OnInternalRenderFrame()
		{
			PluginManager.backendRenderer.currentWindow = windowId;
			PluginManager.backendRenderer.MakeCurrent(handle, contexts[0]);

			TexturePipeline.instance.GenerateGraphicDeviceId();
			MeshPipeline.instance.GenerateGraphicDeviceId();
			ShaderPipeline.instance.GenerateGraphicDeviceId();

			Coroutine.AdvanceInstructions<WaitForMakeCurrent>();

			OnRenderFrame();

			var mainView = MainEditorView.mainViews[windowId];
			if (mainView.desktop is null) return;

			mainView.Render();
			PluginManager.backendRenderer.SwapBuffers(handle);
		}

		public abstract void OnRenderFrame();

		public void OnEvent(SDL.SDL_Event evnt)
		{
			switch (evnt.type)
			{
				case (uint)SDL.SDL_EventType.SDL_EVENT_KEY_DOWN:
					bool combinationMet = true;
					foreach (var command in InputHandler.menuCommands)
					{
						combinationMet = true;
						foreach (var key in command.keyCombination)
						{
							combinationMet = (key) switch
							{
								"CTRL" => evnt.key.mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_LCTRL),
								"SHIFT" => evnt.key.mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_LSHIFT) || evnt.key.mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_RSHIFT),
								_ => evnt.key.key == key.AsSpan()[0]
							};
							if (!combinationMet) break;
						}
						if (combinationMet) { command.Execute(); return; }
					}
					InputHandler.ProcessKeyboard(); break;
				case (uint)SDL.SDL_EventType.SDL_EVENT_KEY_UP:
					// if (evnt.key.keysym.sym == SDL.SDL_Keycode.SDLK_ESCAPE)
					{
						//   quit = MainWindowId == FocusedWindowId;
						//  windows[FocusedWindowId].Close();
					}
					//Console.WriteLine("1 " + (uint)'1' + " : ! " + (uint)'!');

					InputHandler.ProcessKeyboard();
					break;

				case (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
					InputHandler.isMouseDragging = true;
					HitTest(evnt.button.x, evnt.button.y);//use this to fix splitter bars
					InputHandler.ProcessMouse();
					break;

				case (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:
					focusGained = false;
					InputHandler.isMouseDragging = false;
					InputHandler.ProcessMouse();
					break;

				case (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_MOTION:
					InputHandler.ProcessMouseMove();//evnt.motion.xrel instead of

					break;

				case (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_WHEEL: InputHandler.ProcessMouseWheel(evnt.wheel.y); break;
				case (uint)SDL.SDL_EventType.SDL_EVENT_QUIT: quit = true; break;
				/*case (uint)SDL.SDL_EventType.SDL_EVENT_TEXT_INPUT:
					// char types are 8-bit in C, but 16-bit in C#, so we use a byte (8-bit) here
					byte[] rawBytes = new byte[SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE];
					unsafe
					{
						// we have a pointer to an unmanaged character array from the SDL2 lib (event.text.text),
						// so we need to explicitly marshal into our byte array
						//Marshal.Copy((IntPtr)evnt.text.text, rawBytes, 0, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE);
						rawBytes = new Span<byte>(evnt.text.text, SDL.SDL_TEXTINPUTEVENT_TEXT_SIZE).ToArray();
						// the character array is null terminated, so we need to find that terminator
					}
					int indexOfNullTerminator = Array.IndexOf(rawBytes, (byte)0);
					//int indexOfNullTerminator =rawBytes.IndexOf((byte)0);
					// finally, since the character array is UTF-8 encoded, get the UTF-8 string
					string text = System.Text.Encoding.UTF8.GetString(rawBytes, 0, indexOfNullTerminator);
					InputHandler.ProcessTextInput(text);
					break;*/
				default:
					if (evnt.type >= (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_FIRST && evnt.type <= (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_LAST)
						OnWindowEvent(ref evnt.window);
					break;
			}
		}

		public static void OnWindowEvent(ref SDL.SDL_WindowEvent evt)
		{
			switch (evt.type)
			{
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED: if (evt.windowID == MainWindowId) quit = true; else windows[evt.windowID].Close(); break;
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_PIXEL_SIZE_CHANGED:
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_RESIZED:
					if (MainEditorView.mainViews.TryGetValue(evt.windowID, out var mainView))
					{
						mainView.OnResize(evt.data1, evt.data2);
						foreach (var (_, mainV) in MainEditorView.mainViews)
							mainV.desktop.Update();
					}
					Coroutine.AdvanceInstructions<WaitForEndOfFrame>();

					Time.SetTime();
					Coroutine.AdvanceInstructions<WaitForSeconds>();

					Root.removedEntities.Clear();
					Root.addedEntities.Clear();
					//IdReferenceResolver._objectsToId.Clear();
					//IdReferenceResolver._idToObjects.Clear();

					break;

				case SDL.SDL_EventType.SDL_EVENT_WINDOW_EXPOSED:
					PluginManager.backendRenderer.EnableState(RenderState.ScissorTest);
					if (MainEditorView.mainViews.TryGetValue(evt.windowID, out mainView))
						UI.currentCanvas = mainView.desktop;
					foreach (var (_, mainV) in MainEditorView.mainViews)
						mainV.desktop.Draw();
					onRenderFrame?.Invoke();
					//if (windows.Contains(evt.windowID))
					{
						//    windows[evt.windowID].OnInternalRenderFrame();
						//SDL.SDL_GL_SwapWindow(windows[evt.windowID].handle);
					}
					break;

				//case SDL.SDL_EventType.SDL_WINDOWEVENT_TAKE_FOCUS: AssetsView.CheckIfDirTreeChanged(); break;
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER:
					UnderMouseWindowId = evt.windowID;
					if (MainEditorView.mainViews.TryGetValue(evt.windowID, out mainView))
						UI.currentCanvas = mainView.desktop;
					SDL.SDL_CaptureMouse(false); break;//convert to use getglobalmousestate when no events caputred?
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_MOUSE_LEAVE:
					// Console.WriteLine("bu");
					//if (InputHandler.isMouseDragging)
					SDL.SDL_CaptureMouse(true); break;
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_MOVED: break;
				case SDL.SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED: if (windows.ContainsKey(evt.windowID)) windows[evt.windowID].OnFocus(); break;
			}
		}

		private SDL.SDL_HitTestResult HitTest(float x, float y)
		{
			if (x < 5) return SDL.SDL_HitTestResult.SDL_HITTEST_RESIZE_LEFT;
			return SDL.SDL_HitTestResult.SDL_HITTEST_NORMAL;
		}

		public static void OpenView(View view, Control frame)
		{
			TabControl tabcontrol = new TabControl();
			tabcontrol.ButtonFrame.Style = "";
			tabcontrol.Dock = DockStyle.Fill;
			tabcontrol.Parent = frame;
			tabcontrol.PageFrame.Style = "frame";
			tabcontrol.Scissor = true;
			tabcontrol.PageFrame.Padding = new Margin(3, 3, 3, 3);
			tabcontrol.PageFrame.Margin = new Margin(0, -2, 0, 0);

			var tab1 = new TabPage();
			tab1.Button.Text = "test";
			tabcontrol.TabPages.Add(tab1);
			tab1.Scissor = true;
			//tab.Style = "window";
			var tab = view as TabPage;
			tabcontrol.TabPages.Add(tab);
			tabcontrol.SelectedTab = tab;
		}

		public static  unsafe bool OnResize(nint data, SDL.SDL_Event* e)
		{//layers with traits like graphic/ physic/general etc.
			//var evt = Marshal.PtrToStructure<SDL.SDL_Event>(e);
			if(e->type == (uint)SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP)
			{
				focusGained = false;
				InputHandler.isMouseDragging = false;
				InputHandler.ProcessMouse();
			}
			else if(e->type >= (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_FIRST && e->type <= (uint)SDL.SDL_EventType.SDL_EVENT_WINDOW_LAST)
			{
				OnWindowEvent(ref e->window);
			}
			return true;
		}

		public void Hide()
		{
			SDL.SDL_HideWindow(handle);
		}

		public void Close()
		{
			SDL.SDL_DestroyWindow(handle);
			onRenderFrame -= OnInternalRenderFrame;
			windows.Remove(windowId);
		}

		public void Show()
		{
			SDL.SDL_ShowWindow(handle);
		}

		public virtual void OnFocus()
		{
		}
	}
}