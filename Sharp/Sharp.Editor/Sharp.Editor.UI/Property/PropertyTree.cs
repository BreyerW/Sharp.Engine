using System;
using Gwen.ControlInternal;
using Gwen.Control;
using Gwen;

namespace Sharp.Control
{
    /// <summary>
    /// Property table/tree.
    /// </summary>
    public class PropertyTree : TreeControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyTree"/> class.
        /// </summary>
        /// <param name="parent">Parent control.</param>
        public PropertyTree(Base parent)
            : base(parent)
        {
        }

        /// <summary>
        /// Adds a new properties node.
        /// </summary>
        /// <param name="label">Node label.</param>
        /// <returns>Newly created control</returns>
        public Properties Add(string label)
        {
            TreeNode node = new PropertyTreeNode(this);
            node.Text = label;
            node.Dock = Pos.Top;

            Properties props = new Properties(node);
            props.Dock = Pos.Top;

            return props;
        }

        public Properties AddOrGet(string label, out bool existed)
        {
            existed = false;
            foreach (var child in Children)
                if (child is PropertyTreeNode node && node.Text == label)
                {
                    existed = true;
                    return node.Children[0] as Properties;
                }

            return Add(label);
        }
    }
}