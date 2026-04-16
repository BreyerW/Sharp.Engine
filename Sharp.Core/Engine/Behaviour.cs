using Sharp.Core;
using Sharp.Editor.Views;
using System;
using System.Runtime.CompilerServices;

namespace Sharp
{
	public abstract class Behaviour : Component
	{

		internal virtual void OnUpdate()
		{

		}
		internal sealed override void OnActiveChanged()
		{
			if (active)
				SceneView.OnUpdate += OnUpdate;
			else
				SceneView.OnUpdate -= OnUpdate;
		}
	}
}

