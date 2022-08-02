using Sharp.Core;
using Sharp.Editor.Views;
using System;
using System.Runtime.CompilerServices;

namespace Sharp
{
	public abstract class Behaviour : Component
	{
		[ModuleInitializer]
		internal static void Register()
		{
			ref var mask = ref StaticDictionary<Behaviour>.Get<BitMask>();
			if (mask.IsDefault)
				mask = new BitMask(0);
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

