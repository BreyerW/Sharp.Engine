using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using SharpAsset.Pipeline;
using Gwen.Control;
using OpenTK.Input;
using Sharp.Editor.Attribs;

namespace Sharp.Editor.Views
{
	public class AssetsView:View
	{
		public static bool isDragging = false;

		private static readonly DirectoryInfo root = new DirectoryInfo(Environment.CurrentDirectory);
		public static TreeControl tree;

		public override void Initialize ()
		{
			Console.WriteLine (Environment.CurrentDirectory);
			base.Initialize ();
				tree = new  Gwen.Control.TreeControl (canvas);

				tree.SetSize (canvas.Width, canvas.Height);
				tree.ShouldDrawBackground = false;

				var type = typeof(Pipeline);
				var subclasses = type.Assembly.GetTypes ().Where (t => t.IsSubclassOf (type));

				foreach (var subclass in subclasses) {
					subclass.GetCustomAttributes (false);
				}
				MeshPipeline.SetMeshContext<ushort,BasicVertexFormat> ();
				RecursiveBuildTree (root, tree);
				foreach (FileInfo file in FilterFiles(root,SupportedFileFormatsAttribute.importers.Keys)) {
					if (!file.Name.StartsWith ("."))
					tree.AddNode (()=>SupportedFileFormatsAttribute.importers [file.Extension].Import (file.FullName));
				}
			//tree.Show ();
		}
		public override void Render ()
		{
			//base.Render ();
			//GL.Clear(ClearBufferMask.DepthBufferBit|ClearBufferMask.ColorBufferBit);
			/*GL.ClearColor (new OpenTK.Graphics.Color4(255,255,255,255));
			GL.Clear(ClearBufferMask.DepthBufferBit|ClearBufferMask.ColorBufferBit);

			canvas.RenderCanvas();*/
		}
		public override void OnMouseDown(MouseButtonEventArgs evnt){

			isDragging = true;
			//canvas.NeedsRedraw = true;
		}
		public override void OnMouseUp(MouseButtonEventArgs evnt){
			isDragging = false;
			//canvas.NeedsRedraw = true;
			//Console.WriteLine ("up");
		}
		public override void OnMouseMove (MouseMoveEventArgs evnt)
		{
			//if (isDragging && Selection.assets.Count==0 && tree.SelectedChildren.Any ())
			//	foreach (var asset in tree.SelectedChildren)
			//		Selection.assets.Add (asset.Content);
			
			//canvas.NeedsRedraw = true;
		}
		void RecursiveBuildTree(DirectoryInfo dirRoot, Gwen.Control.TreeNode treeIter){
			MeshPipeline.SetMeshContext<ushort,BasicVertexFormat> ();
			foreach (DirectoryInfo di in dirRoot.EnumerateDirectories()) {
				if (!di.Name.StartsWith (".")) {
					var iter=treeIter.AddNode (di.Name);
					foreach (FileInfo file in FilterFiles(di,SharpAsset.Pipeline.SupportedFileFormatsAttribute.importers.Keys))
					{
						if (SupportedFileFormatsAttribute.importers.ContainsKey(file.Extension) && !file.Name.StartsWith("."))
							iter.AddNode(()=>SupportedFileFormatsAttribute.importers[file.Extension].Import(file.FullName));
					}
					RecursiveBuildTree (di, iter);
				}
			}
		}
		public IEnumerable<FileInfo> FilterFiles(DirectoryInfo di,IEnumerable<string> exts) {
			return 
				exts.Select(x => "*" + x) // turn into globs
					.SelectMany(x => 
						di.EnumerateFiles(x)
					);
		}
	}
}

