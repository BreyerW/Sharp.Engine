using Sharp.Engine.Components;
using Squid;
using System;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
	public class SceneStructureView : View//TODO: when clicking structure components dont show up
	{
		internal static TreeView tree;
		internal static Dictionary<Guid, TreeNodeLabel> flattenedTree = new Dictionary<Guid, TreeNodeLabel>();
		public SceneStructureView(uint attachToWindow) : base(attachToWindow)
		{
			if (tree == null)
				tree = new TreeView();
			tree.Parent = this;
			tree.Dock = DockStyle.Fill;
			tree.SelectedNodeChanged += Tree_SelectedNodeChanged;

			Selection.OnSelectionChange += (old, n) =>
			{
				//SceneStructureView.tree.SelectedNode = SceneStructureView.flattenedTree[entity.GetInstanceID()];
				tree.SelectedNodeChanged -= Tree_SelectedNodeChanged;
				tree.SelectedNode = n is null ? null : flattenedTree[n.GetInstanceID()];
				tree.SelectedNodeChanged += Tree_SelectedNodeChanged;
			};
			Button.Text = "Scene Structure";
		}

		private void Tree_SelectedNodeChanged(Control sender, TreeNode value)
		{
			if (value is null || Selection.Asset == value.UserData) return;
			Selection.Asset = value.UserData;

			// var node = sender as TreeNode; if (Selection.Asset == node.Content) tree.UnselectAll();
			// else
		}

		internal static void ReconstructTree(Entity obj)
		{
			tree.Nodes.Clear();
			flattenedTree.Clear();
			tree.SelectedNode = null;
			//RegisterEntity(Camera.main.entityObject, Camera.main.entityObject == obj);
			foreach (var entity in Root.root)
				RegisterEntity(entity, entity == obj);
		}
		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();
			if (Root.addedEntities.Count > 0)
				foreach (var added in Root.addedEntities)
					if (added is Entity e)
						RegisterEntity(e, false);
			if (Root.removedEntities.Count > 0)
				ReconstructTree(tree.SelectedNode?.UserData as Entity);
		}
		private static void RegisterEntity(Entity ent, bool selected)
		{
			//var id=SceneView.entities.IndexOf (ent);
			var node = new TreeNodeLabel();
			node.Label.Text = ent.name;
			node.Label.TextAlign = Alignment.MiddleLeft;
			node.Style = "label";
			node.Label.Style = "label";
			node.UserData = ent;
			if (ent.childs?.Count is 0)
				node.Button.Style = "";
			if (!flattenedTree.ContainsKey(ent.GetInstanceID()))
				flattenedTree.Add(ent.GetInstanceID(), node);
			tree.Nodes.Add(node);
			if (selected)
				tree.SelectedNode = node;
		}
	}
}