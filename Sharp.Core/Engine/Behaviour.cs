using System;
using Sharp.Editor.Views;

namespace Sharp
{
	public class Behaviour : Component
	{
		public Behaviour(Entity parent) : base(parent)
		{
		}

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

