using System;
using System.Collections;
using System.Reflection;
using Squid;

namespace Sharp.Editor.UI.Property
{
	internal class ArrayDrawer : PropertyDrawer<IList>
	{
		public TreeView arrayTree = new TreeView() { Dock = DockStyle.Fill };

		public override IList Value
		{
			get => null;
			set
			{
				arrayTree.Nodes.Clear();

				var node = new TreeNodeLabel();
				node.Label.Text = "0";
				arrayTree.Nodes.Add(node);
				foreach (var val in value)
				{
					if (val is IList list) IterateRecursively(list, node, 1);
					else
					{
						var tmpNode = new TreeNodeLabel();
						tmpNode.Label.Text = val.ToString();
						node.Nodes.Add(tmpNode);
					}
				}
			}
		}

		public ArrayDrawer(MemberInfo memInfo) : base(memInfo)
		{
		}

		private void IterateRecursively(IList list, TreeNode node, int depth)
		{
			if (depth is 8) return;
			foreach (var val in list)
			{
				var tmpNode = new TreeNodeLabel();
				tmpNode.Label.Text = depth.ToString();
				if (val is IList tmpList) IterateRecursively(tmpList, node, depth + 1);
				else
				{
					tmpNode = new TreeNodeLabel();
					tmpNode.Label.Text = val.ToString();
					node.Nodes.Add(tmpNode);
				}
			}
		}
	}
}