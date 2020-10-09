using Sharp.Editor.Views;
using System;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		protected Renderer(Entity parent) : base(parent)
		{
		}

		public abstract void Render();

		internal override void OnActiveChanged()
		{
		}
	}
}