using System;
using Sharp.Editor.Views;

namespace Sharp
{
	public class Behaviour : Component
	{
		internal virtual void OnUpdate()
		{

		}
		internal sealed override void OnActiveChanged()
		{
			if (enabled)
				SceneView.OnUpdate += OnUpdate;
			else
				SceneView.OnUpdate -= OnUpdate;
		}
	}
}

