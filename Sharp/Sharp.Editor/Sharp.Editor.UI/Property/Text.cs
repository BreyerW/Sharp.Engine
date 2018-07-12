using Squid;

namespace Sharp.Editor.UI.Property
{
	/// <summary>
	/// Text property.
	/// </summary>
	public class Text : PropertyDrawer<string>
	{
		protected readonly TextBox m_TextBox;

		public Text(string name) : base(name)
		{
			m_TextBox = new TextBox();
			m_TextBox.Position = new Point(label.Size.x, 0);
			Childs.Add(m_TextBox);
		}

		/// <summary>
		/// Property value.
		/// </summary>
		public override string Value
		{
			get { return m_TextBox.Text; }
			set { m_TextBox.Text = value; }
		}
	}
}