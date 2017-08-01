using System;
using Squid;

namespace Sharp.Editor.Views
{
    public class SceneStructureView : View
    {
        public static TreeView tree;

        public SceneStructureView(uint attachToWindow) : base(attachToWindow)
        {
            if (tree == null)
                tree = new TreeView();
            tree.Parent = this;
            tree.Dock = DockStyle.Fill;
            tree.SelectedNodeChanged += Tree_SelectedNodeChanged;

            SceneView.OnAddedEntity += ReconstructTree;
            SceneView.OnRemovedEntity += ReconstructTree;
            Name = "Scene Structure";
            ReconstructTree();
        }

        private void Tree_SelectedNodeChanged(Control sender, TreeNode value)
        {
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
    }
}