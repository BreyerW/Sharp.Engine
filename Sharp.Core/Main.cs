using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SDL2;
using Sharp.Editor;
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

			var rotationX = Matrix4x4.CreateFromAxisAngle(Vector3.UnitX, NumericsExtensions.Deg2Rad * 90);
			var rotationY = Matrix4x4.CreateFromAxisAngle(Vector3.UnitZ, NumericsExtensions.Deg2Rad * 90);
			var rotationZ = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, -NumericsExtensions.Deg2Rad * 90);

			CreatePrimitiveMesh.numVertices = 33;
			var gizmo = GenerateArrows(rotationX, rotationY, rotationZ);
			GeneratePlanes(ref gizmo,rotationX, rotationY, rotationZ);
			GenerateCircles(ref gizmo, rotationX, rotationY, rotationZ);
			GenerateCubes(ref gizmo, rotationX, rotationY, rotationZ);

			Pipeline.Get<Mesh>().Register(gizmo);
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCube(Matrix4x4.Identity));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCylinder(Matrix4x4.Identity));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateCone(Matrix4x4.Identity));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateTorus(Matrix4x4.Identity));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare(Matrix4x4.Identity));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare(Matrix4x4.CreateScale(2) * Matrix4x4.CreateTranslation(-1, -1, 0), "screen_space_square"));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateSquare(Matrix4x4.CreateTranslation(-0.5f, -0.5f, 0), "negative_square"));
			Pipeline.Get<Mesh>().Register(CreatePrimitiveMesh.GenerateLine());
			var dSquare = CreatePrimitiveMesh.GenerateSquare(Matrix4x4.Identity, "dynamic_square");
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
		private static Mesh GenerateArrows(in Matrix4x4 rotationX, in Matrix4x4 rotationY, in Matrix4x4 rotationZ)
		{
			var arrow = CreatePrimitiveMesh.GenerateCylinder(Matrix4x4.CreateScale(15, 1, 1) * rotationX, "gizmo", Manipulators.xColor);

			var cone = CreatePrimitiveMesh.GenerateCone(Matrix4x4.CreateScale(5, 4, 4) * Matrix4x4.CreateTranslation(15, 0, 0) * rotationX, vertexColor: Manipulators.xColor);
			arrow.AddSubMesh(ref cone, true);

			var arrowY = CreatePrimitiveMesh.GenerateCylinder(Matrix4x4.CreateScale(15, 1, 1) * rotationY, vertexColor: Manipulators.yColor);

			cone = CreatePrimitiveMesh.GenerateCone(Matrix4x4.CreateScale(5, 4, 4) * Matrix4x4.CreateTranslation(15, 0, 0) * rotationY, vertexColor: Manipulators.yColor);
			arrowY.AddSubMesh(ref cone, true);
			arrow.AddSubMesh(ref arrowY);
			var arrowZ = CreatePrimitiveMesh.GenerateCylinder(Matrix4x4.CreateScale(15, 1, 1) * rotationZ, vertexColor: Manipulators.zColor);

			cone = CreatePrimitiveMesh.GenerateCone(Matrix4x4.CreateScale(5, 4, 4) * Matrix4x4.CreateTranslation(15, 0, 0) * rotationZ, vertexColor: Manipulators.zColor);
			arrowZ.AddSubMesh(ref cone, true);
			arrow.AddSubMesh(ref arrowZ);
			return arrow;
		}
		private static void GenerateCubes(ref Mesh gizmo,in Matrix4x4 rotationX, in Matrix4x4 rotationY, in Matrix4x4 rotationZ)
		{
			var scaleCube = CreatePrimitiveMesh.GenerateCube(Matrix4x4.CreateScale(4) * Matrix4x4.CreateTranslation(20, -2, -2) * rotationX, vertexColor: Manipulators.xColor);
			var scaleCubeY = CreatePrimitiveMesh.GenerateCube(Matrix4x4.CreateScale(4) * Matrix4x4.CreateTranslation(20, -2, -2) * rotationY, vertexColor: Manipulators.yColor);
			var scaleCubeZ = CreatePrimitiveMesh.GenerateCube(Matrix4x4.CreateScale(4) * Matrix4x4.CreateTranslation(20, -2, -2) * rotationZ, vertexColor: Manipulators.zColor);
			gizmo.AddSubMesh(ref scaleCube);
			gizmo.AddSubMesh(ref scaleCubeY);
			gizmo.AddSubMesh(ref scaleCubeZ);
		}
		private static void GeneratePlanes(ref Mesh gizmo, in Matrix4x4 rotationX, in Matrix4x4 rotationY, in Matrix4x4 rotationZ)
		{
			var fixRotation = Matrix4x4.CreateFromAxisAngle(Vector3.UnitY, NumericsExtensions.Deg2Rad * 180);
			var gizmo_square = CreatePrimitiveMesh.GenerateSquare(Matrix4x4.CreateScale(7) * Matrix4x4.CreateTranslation(2.5f, 2.5f, 0) * rotationZ, vertexColor: Manipulators.xColor);
			
			var gizmo_squareY = CreatePrimitiveMesh.GenerateSquare(Matrix4x4.CreateScale(7) * Matrix4x4.CreateTranslation(2.5f, 2.5f, 0) * rotationX, vertexColor: Manipulators.yColor);

			var gizmo_squareZ = CreatePrimitiveMesh.GenerateSquare(Matrix4x4.CreateScale(7) * Matrix4x4.CreateTranslation(2.5f, 2.5f, 0) * rotationY * fixRotation, vertexColor: Manipulators.zColor);
			
			gizmo.AddSubMesh(ref gizmo_squareZ);
			gizmo.AddSubMesh(ref gizmo_square);
			gizmo.AddSubMesh(ref gizmo_squareY);
		}
		private static void GenerateCircles(ref Mesh gizmo, in Matrix4x4 rotationX, in Matrix4x4 rotationY, in Matrix4x4 rotationZ)
		{
			var circle = CreatePrimitiveMesh.GenerateTorus(Matrix4x4.CreateScale(20) * rotationZ, vertexColor: Manipulators.xColor);
			var circleY = CreatePrimitiveMesh.GenerateTorus(Matrix4x4.CreateScale(20) * rotationX, vertexColor: Manipulators.yColor);
			var circleZ = CreatePrimitiveMesh.GenerateTorus(Matrix4x4.CreateScale(20) * rotationY, vertexColor: Manipulators.zColor);
			gizmo.AddSubMesh(ref circle);
			gizmo.AddSubMesh(ref circleY);
			gizmo.AddSubMesh(ref circleZ);
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