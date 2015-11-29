using System;
using Sharp.Editor.Views;

namespace Sharp
{
	public abstract class Renderer: Component
	{
		public abstract void Render ();
		public abstract void SetupMatrices ();

		protected internal sealed override void OnEnableInternal ()
		{
			SceneView.OnRenderFrame +=Render;
			SceneView.OnSetupMatrices += SetupMatrices;
		}
		protected internal sealed override void OnDisableInternal ()
		{
			SceneView.OnRenderFrame -=Render;
			SceneView.OnSetupMatrices -= SetupMatrices;
		}
	}
}

