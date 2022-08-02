using Sharp.Editor.Views;
using Squid;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property

{
	static partial class Registerer
	{
		[ModuleInitializer]
		internal static void Register3()
		{
			InspectorView.RegisterDrawerFor<bool>(() => new BooleanDrawer());

		}
	}
	/// <summary>
	/// Checkable property.
	/// </summary>
	public class BooleanDrawer : PropertyDrawer<bool>
	{
		private CheckBox m_CheckBox;
		public BooleanDrawer() : base()
		{
			m_CheckBox = new CheckBox();
			m_CheckBox.Size = new Point(15, 15);
			m_CheckBox.Position = new Point(label.Size.x + 1, 0);
			Childs.Add(label);
			Childs.Add(m_CheckBox);
			m_CheckBox.Style = "checkBox";
			//m_CheckBox.IsChecked = Value;
			m_CheckBox.CheckedChanged += M_CheckBox_CheckedChanged;
		}
		protected override void OnUpdate()
		{
			if (m_CheckBox is not null)
				m_CheckBox.IsChecked = (bool)Value;
			base.OnUpdate();

		}
		private void M_CheckBox_CheckedChanged(Control sender)
		{
			Value = m_CheckBox.IsChecked;
			//if (Name is "enabled")
			//	Target.OnActiveChanged();
		}
	}
}