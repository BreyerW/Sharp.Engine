using Squid;

namespace Sharp.Editor.UI.Property

{
	/// <summary>
	/// Checkable property.
	/// </summary>
	public class BooleanDrawer : PropertyDrawer<bool>
	{
		private CheckBox m_CheckBox;

		public BooleanDrawer(string name)
			 : base(name)
		{
			m_CheckBox = new CheckBox();
			m_CheckBox.Size = new Point(15, 15);
			m_CheckBox.Position = new Point(label.Size.x + 1, 0);
			Childs.Add(label);
			Childs.Add(m_CheckBox);
			m_CheckBox.Style = "checkBox";
		}

		/// <summary>
		/// Property value.
		/// </summary>
		public override bool Value
		{
			get => m_CheckBox.IsChecked;
			set { m_CheckBox.IsChecked = value; }
		}
	}
}