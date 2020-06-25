using Newtonsoft.Json.Linq;
using Squid;
using System.Reflection;

namespace Sharp.Editor.UI.Property

{
	/// <summary>
	/// Checkable property.
	/// </summary>
	public class BooleanDrawer : PropertyDrawer<bool>
	{
		private CheckBox m_CheckBox;
		public BooleanDrawer(MemberInfo memInfo) : base(memInfo)
		{
			Update += BooleanDrawer_Layout;
		}

		private void BooleanDrawer_Layout(Control sender)
		{
			if (m_CheckBox is null)
			{
				m_CheckBox = new CheckBox();
				m_CheckBox.Size = new Point(15, 15);
				m_CheckBox.Position = new Point(label.Size.x + 1, 0);
				Childs.Add(label);
				Childs.Add(m_CheckBox);
				m_CheckBox.Style = "checkBox";
				//m_CheckBox.IsChecked = serializedObject.Value<bool>();
				m_CheckBox.CheckedChanged += M_CheckBox_CheckedChanged;
			}
		}
		protected override void OnUpdate()
		{
			if (m_CheckBox is { })
				m_CheckBox.IsChecked = SerializedObject.Value<bool>();
			base.OnUpdate();

		}
		private void M_CheckBox_CheckedChanged(Control sender)
		{
			SerializedObject = m_CheckBox.IsChecked;
		}
	}
}