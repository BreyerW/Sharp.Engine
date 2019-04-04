using System;
using Sharp.Editor.Views;

namespace Sharp
{
	public class Behaviour:Component
	{
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
		public Behaviour ()
		{
		}
	}
}

