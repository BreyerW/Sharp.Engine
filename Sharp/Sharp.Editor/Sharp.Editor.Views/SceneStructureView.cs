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
            //tree.SetSize(panel.Width, panel.Height);
            tree.Dock = DockStyle.Fill;
            //tree.SelectionChanged += (sender, args) => Selection.Asset = (sender as TreeNode).Content;

            //var node = sender as TreeNode; if ( Selection.Asset ==node.Content) tree.UnselectAll(); else
            SceneView.OnAddedEntity += ReconstructTree;
            SceneView.OnRemovedEntity += ReconstructTree;
            ReconstructTree();
        }

        private void ReconstructTree()
        {
            //tree.RemoveAll();
            RegisterEntity(Camera.main.entityObject);
            foreach (var entity in SceneView.entities)
                RegisterEntity(entity);
        }

        public override void Render()
        {
            //base.Render();
        }

        private static void RegisterEntity(Entity ent)
        {
            //var id=SceneView.entities.IndexOf (ent);
            var node = new TreeNodeLabel();
            node.Label.Text = ent.name;
            node.Label.TextAlign = Alignment.MiddleLeft;
            node.Style = "label";
            node.Label.Style = "label";
            tree.Nodes.Add(node);
        }

        public override void OnMouseMove(OpenTK.Input.MouseMoveEventArgs evnt)
        {
            base.OnMouseMove(evnt);
        }

        public override void OnResize(int width, int height)
        {
            //tree.SetSize(panel.Width, panel.Height);
        }
    }
}