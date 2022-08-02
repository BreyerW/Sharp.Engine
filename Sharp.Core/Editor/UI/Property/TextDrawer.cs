using Sharp.Editor.Views;
using Squid;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property
{
	static partial class Registerer
	{
		[ModuleInitializer]
		internal static void Register4()
		{
			InspectorView.RegisterDrawerFor<string>(() => new TextDrawer());
		}
	}
	/// <summary>
	/// Text property.
	/// </summary>
	public class TextDrawer : PropertyDrawer<string>
	{
		protected readonly TextField m_TextBox;

		public TextDrawer() : base()
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