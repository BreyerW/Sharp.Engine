using System;
using Squid;

namespace Sharp.Editor.Views
{
    public class SceneStructureView : View
    {
        public static TreeView tree;

        public SceneStructureView(uint attachToWindow) : base(attachToWindow)
        {
        }

        public override void Initialize()
        {
            base.Initialize();

            if (tree == null)
                tree = new TreeView();
            tree.Parent = panel;
            tree.Dock = DockStyle.Fill;
            tree.SelectedNodeChanged += Tree_SelectedNodeChanged;

            SceneView.OnAddedEntity += ReconstructTree;
            SceneView.OnRemovedEntity += ReconstructTree;
            ReconstructTree();
        }

        private void Tree_SelectedNodeChanged(Squid.Control sender, TreeNode value)
        {
            Console.WriteLine(value);
            if (value is null) return;
            Selection.Asset = value.UserData;

            // var node = sender as TreeNode; if (Selection.Asset == node.Content) tree.UnselectAll();
            // else
        }

        private void ReconstructTree()
        {
            tree.Nodes.Clear();
            RegisterEntity(Camera.main.entityObject);
            foreach (var entity in SceneView.entities)
                RegisterEntity(entity);
        }

        public override void Render()
        {
        }

        private static void RegisterEntity(Entity ent)
        {
            //var id=SceneView.entities.IndexOf (ent);
            var node = new TreeNodeLabel();
            node.Label.Text = ent.name;
            node.Label.TextAlign = Alignment.MiddleLeft;
            node.Style = "label";
            node.Label.Style = "label";
            node.UserData = ent;
            if (ent.childs.Count is 0)
                node.Button.Style = "";
            tree.Nodes.Add(node);
        }

        public override void OnMouseMove(OpenTK.Input.MouseMoveEventArgs evnt)
        {
            base.OnMouseMove(evnt);
        }

        public override void OnResize(int width, int height)
        {
        }
    }
}