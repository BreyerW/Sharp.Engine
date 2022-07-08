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

			ListWrapper.OnListChange += (n) =>
			{
				//SceneStructureView.tree.SelectedNode = SceneStructureView.flattenedTree[entity.GetInstanceID()];

				tree.SelectedNodeChanged -= Tree_SelectedNodeChanged;
				if (n.Count is 0)
					tree.SelectedNode = null;
				else
					foreach (var o in n)
					{
						flattenedTree[o.GetInstanceID()].IsSelected = true;
					}
				tree.SelectedNodeChanged += Tree_SelectedNodeChanged;
			};
			Button.Text = "Scene Structure";
		}

		private void Tree_SelectedNodeChanged(Control sender, TreeNode value)
		{
			if (value is null || Selection.selectedAssets.Contains(value.UserData)) return;

			/*
			 if (value is null)
			{
				Manipulators.SelectedGizmoId = Gizmo.Invalid;
				Manipulators.hoveredGizmoId = Gizmo.Invalid;
				return;
			}
			if (Selection.Asset == value.UserData) return;
			 */
			Selection.selectedAssets.ReplaceWithOne(value.UserData);
			//Selection.selectedAssets.RaiseEvent();
			// var node = sender as TreeNode; if (Selection.Asset == node.Content) tree.UnselectAll();
			// else
		}
		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();
			foreach (var removed in Root.removedEntities)
			{
				if (removed is Entity)
				{
					var id = removed.GetInstanceID();
					tree.Nodes.Remove(flattenedTree[id]);
					flattenedTree.Remove(id);
				}
			}
			foreach (var added in Root.addedEntities)
				if (added is Entity e)
					RegisterEntity(e, false);
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
			//if (!flattenedTree.ContainsKey(ent.GetInstanceID()))
			flattenedTree.TryAdd(ent.GetInstanceID(), node);
			tree.Nodes.Add(node);
			if (selected)
				tree.SelectedNode = node;
		}
	}
}