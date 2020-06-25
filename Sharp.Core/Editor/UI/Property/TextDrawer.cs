using Squid;
using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	/// <summary>
	/// Text property.
	/// </summary>
	public class TextDrawer : PropertyDrawer<string>
	{
		protected readonly TextField m_TextBox;

		public TextDrawer(MemberInfo memInfo) : base(memInfo)
		{
			m_TextBox = new TextField();
			m_TextBox.Position = new Point(label.Size.x, 0);
			Childs.Add(m_TextBox);
		}

		/// <summary>
		/// Property value.
		/// </summary>
		/*	public override string Value
			{
				get { return m_TextBox.Text; }
				set { m_TextBox.Text = value; }
			}
		}*/
	}
}