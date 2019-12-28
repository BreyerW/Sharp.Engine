using Sharp.Editor.Views;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		public Renderer(Entity parent) : base(parent)
		{
		}

		public abstract void Render();

		protected internal sealed override void OnEnableInternal()
		{
			Extension.entities.OnRenderFrame += Render;//remove this so that sealed could be removed
		}

		protected internal sealed override void OnDisableInternal()
		{
			Extension.entities.OnRenderFrame -= Render;
		}
	}
}