using System;
using Sharp.Editor.Views;

namespace Sharp
{
	public class Behaviour:Component
	{
		public Behaviour(Entity parent) : base(parent)
		{
		}

		internal virtual void OnUpdate(){
		
		}
		protected internal sealed override void OnEnableInternal ()
		{
			SceneView.OnUpdate += OnUpdate;
		}
		protected internal sealed override void OnDisableInternal ()
		{
			SceneView.OnUpdate -= OnUpdate;
		}
	}
}

