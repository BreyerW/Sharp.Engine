using Sharp.Editor.Views;

namespace Sharp
{
	public abstract class Renderer : Component
	{
		public abstract void Render();

		protected internal sealed override void OnEnableInternal()
		{
			SceneView.entities.OnRenderFrame += Render;
		}

		protected internal sealed override void OnDisableInternal()
		{
			SceneView.entities.OnRenderFrame -= Render;
		}
	}
}