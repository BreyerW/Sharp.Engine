using System;
using System.Collections;
using Gwen.Control;
using Gwen.Control.Property;

namespace Sharp.Editor.UI.Property
{
    internal class ArrayDrawer : PropertyDrawer<IList>
    {
        public TreeControl arrayTree;

        public ArrayDrawer(Base parent) : base(parent)
        {
            //Array.Resize(ref Value,)
            arrayTree = new TreeControl(this);
            arrayTree.Dock = Gwen.Pos.Fill;
            arrayTree.BoundsChanged += OnSizeChanged;
            arrayTree.ShouldDrawBackground = false;
        }

        private void OnSizeChanged(object sender, EventArgs args)
        {
            this.Height = arrayTree.GetChildrenSize().Y;
            Parent.Invalidate();
        }

        public override void SetValue(IList value, bool fireEvents = false)
        {
            arrayTree.DeleteAllChildren();
            var node = arrayTree.AddNode("0");
            foreach (var val in value)
            {
                Console.WriteLine("gettype " + val.GetType());
                if (val is IList list) IterateRecursively(list, node.AddNode("1"), 1);
                else
                    node.AddNode(val);
            }
        }

        private void IterateRecursively(IList list, TreeNode node, int depth)
        {
            if (depth is 8) return;
            foreach (var val in list)
            {
                if (val is IList tmpList) IterateRecursively(tmpList, node.AddNode(depth + 1), depth + 1);
                else
                    node.AddNode(val);
            }
        }
    }
}