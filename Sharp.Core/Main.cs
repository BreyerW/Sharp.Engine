using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SDL2;
using Sharp.Editor.Views;
using SharpAsset;
using SharpAsset.Pipeline;
using SharpSL.BackendRenderers.OpenGL;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sharp
{
	internal class MainClass
	{
		internal static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
		{
			ContractResolver = new UninitializedResolver() { IgnoreSerializableAttribute = false },
			Converters = new List<JsonConverter>() { new DictionaryConverter(), /*new EntityConverter(),*/ new DelegateConverter(), new ListReferenceConverter(),/*new IAssetConverter(), new IEngineConverter(),*/new PtrConverter(), new ReferenceConverter() },
			PreserveReferencesHandling = PreserveReferencesHandling.All,
			ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			TypeNameHandling = TypeNameHandling.All,
			ObjectCreationHandling = ObjectCreationHandling.Replace,
			ReferenceResolverProvider = () => new IdReferenceResolver(),
			NullValueHandling = NullValueHandling.Ignore,
			//DefaultValueHandling = DefaultValueHandling.Populate
		};
		public static void Main(string[] args)
		{
			MainEditorView.editorBackendRenderer = new EditorOpenGLRenderer();
			MainWindow.backendRenderer = new OpenGLRenderer();
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

			Pipeline.Initialize();
			CreatePrimitiveMesh.numVertices = 33;
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCylinder());
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCone());
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCube());
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateTorus());
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare("square", new Vector2(0), new Vector2(1)));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare("screen_space_square", new Vector2(-1), new Vector2(1)));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare("negative_square", new Vector2(-0.5f), new Vector2(0.5f)));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateLine());
			var dSquare = CreatePrimitiveMesh.GenerateSquare("dynamic_square", new Vector2(0), new Vector2(1));
			dSquare.UsageHint = UsageHint.DynamicDraw;
			Pipeline.Get<Mesh>().Register(dSquare);
			Pipeline.Get<Font>().Import(@"C:\Windows\Fonts\times.ttf");



			// OpenTK.Graphics.GraphicsContext.ShareContexts = false;
			SDL.SDL_SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
			SDL.SDL_Init(SDL.SDL_INIT_VIDEO);
			//SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_DOUBLEBUFFER, 2);
			//SDL.SDL_GL_SetAttribute(SDL.SDL_GLattr.SDL_GL_CONTEXT_FLAGS, (int)SDL.SDL_GLcontext.);

			MainWindow.backendRenderer.Start();

			var dummy = SDL.SDL_CreateWindow("", 0, 0, 1, 1, SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL); //convert dummy to splash screen?
																																			 //
			SDL.SDL_GL_CreateContext(dummy);
			var id = MainWindow.backendRenderer.CreateContext(SDL.SDL_GL_GetProcAddress, SDL.SDL_GL_GetCurrentContext);
			MainWindow.contexts.Add(id);

			MainWindow.backendRenderer.MakeCurrent += SDL.SDL_GL_MakeCurrent;
			MainWindow.backendRenderer.SwapBuffers += SDL.SDL_GL_SwapWindow;

			var mWin = new MainWindow("test"); //Console.WriteLine("alpha: " + graphic.GraphicsMode.ColorFormat.Alpha);

			mWin.Initialize(new AssetsView(mWin.windowId), new SceneView(mWin.windowId), new SceneStructureView(mWin.windowId), new InspectorView(mWin.windowId));

			//new FloatingWindow("", handle.t);
			//var mWin2 = new MainWindow("test2");
			//mWin2.Initialize(new AssetsView(mWin2.windowId));
			SDL.SDL_DestroyWindow(dummy);
			MainWindow.backendRenderer.EnableScissor();

			Window.PollWindows();
			SDL.SDL_Quit();
		}

	}
	class UninitializedResolver : DefaultContractResolver
	{
		protected override JsonObjectContract CreateObjectContract(Type objectType)
		{
			JsonObjectContract contract = base.CreateObjectContract(objectType);
			contract.DefaultCreator = () =>
			{
				return RuntimeHelpers.GetUninitializedObject(objectType);
			};
			return contract;
		}
	}
}