using System.Reflection;

namespace Sharp.Editor.UI.Property
{
	class InvisibleSentinel<T> : PropertyDrawer<T>
	{
		public InvisibleSentinel(MemberInfo memInfo) : base(memInfo)
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
	}
}
