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

		private static readonly DirectoryInfo root = new DirectoryInfo(@"B:\Sharp_kopia\Sharp\Content");
		public static TreeControl tree;

		public override void Initialize ()
		{
			Console.WriteLine (Environment.CurrentDirectory);
			base.Initialize ();
            tree = new  Gwen.Control.TreeControl (panel);
            
            tree.SetSize (panel.Width, panel.Height);
				tree.ShouldDrawBackground = false;

				var type = typeof(Pipeline);
				var subclasses = type.Assembly.GetTypes ().Where (t => t.IsSubclassOf (type));

				foreach (var subclass in subclasses) {
					subclass.GetCustomAttributes (false);
				}
				MeshPipeline.SetMeshContext<ushort,BasicVertexFormat> ();
				RecursiveBuildTree (root, tree);
				foreach (FileInfo file in FilterFiles(root,SupportedFileFormatsAttribute.importers.Keys)) {
				if (!file.Name.StartsWith (".") && SupportedFileFormatsAttribute.importers.ContainsKey(file.Extension))
					tree.AddNode (SupportedFileFormatsAttribute.importers [file.Extension].Import (file.FullName));
				}
            
            tree.Show ();
            tree.Selected += OnSelected;
        }

        private void OnSelected(Base sender, EventArgs arguments)
        {
           // Console.WriteLine("clicked");
        }

        public override void Render ()
		{
		}
		public override void OnMouseDown(MouseButtonEventArgs evnt){
            Console.WriteLine("clicked");
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
		public override void OnResize(int width, int height)
		{
			tree.SetSize(panel.Width,panel.Height);
		}
		void RecursiveBuildTree(DirectoryInfo dirRoot, Gwen.Control.TreeNode treeIter){
			MeshPipeline.SetMeshContext<ushort,BasicVertexFormat> ();
			foreach (DirectoryInfo di in dirRoot.EnumerateDirectories()) {
				if (!di.Name.StartsWith (".")) {
					var iter=treeIter.AddNode (di.Name);
					foreach (FileInfo file in FilterFiles(di,SharpAsset.Pipeline.SupportedFileFormatsAttribute.importers.Keys))
					{
						if (SupportedFileFormatsAttribute.importers.ContainsKey(file.Extension) && !file.Name.StartsWith("."))
							iter.AddNode(SupportedFileFormatsAttribute.importers[file.Extension].Import(file.FullName));
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

