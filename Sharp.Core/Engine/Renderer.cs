using Sharp.Editor.Views;
using System;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		public abstract void Render();

		internal override void OnActiveChanged()
		{
		}
	}
}