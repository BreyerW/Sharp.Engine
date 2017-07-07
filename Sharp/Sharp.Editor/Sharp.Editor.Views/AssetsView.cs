using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;
using SharpAsset.Pipeline;
using Squid;
using OpenTK.Input;
using TupleExtensions;

namespace Sharp.Editor.Views
{
    public class AssetsView : View//TODO: nested entity, complete gizmo
    {
        internal static readonly DirectoryInfo root = new DirectoryInfo(Environment.CurrentDirectory + "\\Content"); //Path.GetFullPath(@"..\..\")
        private static Dictionary<string, ConcurrentDictionary<string, FileInfo>> directories = new Dictionary<string, ConcurrentDictionary<string, FileInfo>>();//IAsset ordered by name
        private static readonly FileSystemWatcher dirWatcher = new FileSystemWatcher(root.FullName);
        private static HashSet<string> eventsOnceFired = new HashSet<string>();

        public static Dictionary<uint, TreeView> tree = new Dictionary<uint, TreeView>();
        public static bool isDragging = false;
        public static uint whereDragStarted;
        public static bool rebuildDirTree = false;

        static AssetsView()
        {
            dirWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            dirWatcher.IncludeSubdirectories = true;
            dirWatcher.EnableRaisingEvents = true;
        }

        public AssetsView(uint attachToWindow) : base(attachToWindow)
        {
            //dirWatcher.Created += OnFileOrDirChanged;
            //dirWatcher.Changed += OnFileOrDirChanged;
            //dirWatcher.Deleted += OnFileOrDirDeleted;
        }

        public override void Initialize()
        {
            base.Initialize();
            tree.Add(attachedToWindow, new TreeView());
            tree[attachedToWindow].Dock = DockStyle.Fill;
            tree[attachedToWindow].Parent = panel;
            tree[attachedToWindow].Indent = 10;
            /*var type = typeof(GenericPipeline<>);
            var subclasses = type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));

            foreach (var subclass in subclasses)
            {
                subclass.GetCustomAttributes(false);
            }*/
            if (directories.Count == 0)
                ConstructFlatDirectoryTree(root);
            BuildAssetViewTree();
            tree[attachedToWindow].IsVisible = true;
            tree[attachedToWindow].SelectedNodeChanged += T_SelectedNodeChanged;
            //tree[attachedToWindow].Selected += OnSelected;
        }

        /*  private void OnFileOrDirDeleted(object sender, FileSystemEventArgs e)
          {
              var extension = Path.GetExtension(e.FullPath);
              var isDirectory = Directory.Exists(e.FullPath);
              if (isDirectory)
                  directories.Remove(e.FullPath);
              else
                  directories[e.FullPath].TryRemove(e.Name, out _);
              //tree[attachedToWindow].RemoveChild(tree[attachedToWindow].FindNodeByText(e.Name.Remove(e.Name.Length - extension.Length)), false);
              //tree[attachedToWindow].Redraw();
              dirTreeChanged = true;
          }

          private void OnFileOrDirChanged(object sender, FileSystemEventArgs e)
          {
              lock (eventsOnceFired)
              {
                  if (!eventsOnceFired.Contains(e.FullPath) || !IsFileReady(e.FullPath))
                  {
                      eventsOnceFired.Add(e.FullPath);
                      return;
                  }
              }
              var isDirectory = Directory.Exists(e.FullPath);
              var extension = Path.GetExtension(e.FullPath);
              Console.WriteLine("tuple " + (e.FullPath, extension));
              if (!isDirectory && Pipeline.allPipelines.ContainsKey(extension))
              {
                  var asset = new FileInfo(e.FullPath);
                  directories[e.FullPath.Remove(e.FullPath.Length - e.Name.Length)].AddOrUpdate(e.Name.Remove(e.Name.Length - extension.Length), asset, (key, value) => asset);
                  //var node = tree[attachedToWindow].AddNode((e.FullPath, extension));
                  //node.Text = asset.Name;
              }
              //else
                  //directories.Keys[e.]= e.FullPath;

              eventsOnceFired.Remove(e.FullPath);
          }*/

        public static bool IsFileReady(string sFilename)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(sFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return inputStream.Length > 0;
                }
            }
            catch (IOException)
            {
                return false;
            }
        }

        private void OnSelected(Squid.Control sender, EventArgs arguments)
        {
            Console.WriteLine("clicked" + sender);
            isDragging = true;
            whereDragStarted = attachedToWindow;
        }

        public override void OnMouseDown(int buttonId)
        {
            //canvas.NeedsRedraw = true;
        }

        public override void OnMouseUp(int buttonId)
        {
            isDragging = false;
            //canvas.NeedsRedraw = true;
            //Console.WriteLine ("up");
        }

        public override void OnMouseMove(MouseMoveEventArgs evnt)
        {
            //if (isDragging && Selection.assets.Count==0 && tree.SelectedChildren.Any ())
            //	foreach (var asset in tree.SelectedChildren)
            //		Selection.assets.Add (asset.Content);

            //canvas.NeedsRedraw = true;
        }

        public override void OnResize(int width, int height)
        {
            //tree[attachedToWindow].SetSize(panel.Width, panel.Height);
        }

        public static void CheckIfDirTreeChanged()
        {
            var dirs = Directory.EnumerateDirectories(root.FullName, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                if (!directories.ContainsKey(dir))
                {
                    directories.Add(dir, new ConcurrentDictionary<string, FileInfo>());
                    foreach (var file in new DirectoryInfo(dir).EnumerateFiles("*", SearchOption.AllDirectories).Where((file) => !file.Extension.Equals(".bin", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        //Pipeline.GetPipeline(file.Extension).Import(file.FullName);
                        directories[file.DirectoryName].TryAdd(file.Name, file);
                    }
                }
            }
            var deletedDirs = directories.Keys.Except(dirs);
            foreach (var dirToDelete in deletedDirs)
                directories.Remove(dirToDelete);
        }

        private void ConstructFlatDirectoryTree(DirectoryInfo dirRoot)
        {
            Console.WriteLine("Start directories");
            directories.Add(dirRoot.FullName, new ConcurrentDictionary<string, FileInfo>());
            foreach (var dir in dirRoot.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                directories.Add(dir.FullName, new ConcurrentDictionary<string, FileInfo>());
            }
            Parallel.ForEach(dirRoot.EnumerateFiles("*", SearchOption.AllDirectories).Where((file) => !file.Extension.Equals(".bin", StringComparison.InvariantCultureIgnoreCase)), (item) =>
               {
                   if (Pipeline.IsExtensionSupported(item.Extension))
                   {
                       //Pipeline.GetPipeline(item.Extension).Import(item.FullName);
                       directories[item.DirectoryName].TryAdd(item.Name, item);
                   }
               });
        }

        private void BuildAssetViewTree()//rebuild on focusgained
        {
            var t = tree[attachedToWindow];
            t.Style = "treeview";

            foreach (var (key, files) in directories)
            {
                TreeNodeLabel node = new TreeNodeLabel();
                foreach (var folder in key.Split(new string[] { root.Parent.FullName + @"\", @"\" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (folder != @"Content")
                    {
                        node = t.Nodes.Find((n) => n.Name == folder) as TreeNodeLabel;

                        if (node is null)
                        {
                            node = new TreeNodeLabel();
                            node.Style = "label";
                            node.Label.Text = folder;
                            node.Label.Style = "label";
                            node.Name = folder;
                            node.Label.TextAlign = Alignment.MiddleLeft;
                            t.Nodes.Add(node);
                        }
                    }
                    //node = (TreeNode)node.FindChildByName(folder) ?? node.AddNode(folder);
                }

                foreach (var asset in files.Values.OrderBy(item => item, new CompareByName()))
                {
                    var assetNode = new TreeNodeLabel();
                    assetNode.Name = asset.Name.Remove(asset.Name.Length - asset.Extension.Length);
                    assetNode.Label.Text = assetNode.Name;
                    assetNode.Label.TextAlign = Alignment.MiddleLeft;
                    assetNode.Style = "label";
                    assetNode.Label.Style = "label";
                    node.Nodes.Add(assetNode);//.Content = (asset.FullName, asset.Extension);
                }
            }
        }

        private void T_SelectedNodeChanged(Squid.Control sender, TreeNode value)
        {
            if (value is null) return;
            value.Selected = true;
        }
    }

    internal class CompareByName : IComparer<FileInfo>
    {
        public int Compare(FileInfo x, FileInfo y)
        {
            return string.Compare(x.Name, y.Name);
        }
    }
}