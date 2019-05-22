using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	class InvisibleSentinel : PropertyDrawer<object>
	{
		public InvisibleSentinel(string name, MemberInfo memInfo) : base(name, memInfo)
		{
			NoEvents = true;
			Style = "";
			IsVisible = false;
		}
		protected override void Draw()
		{
		}
		protected override void DrawAfter()
		{
		}
		protected override void DrawBefore()
		{
		}
		public override object Value { get => getter((Parent.Parent as ComponentNode).referencedComponent); set { return; } }
	}
}
