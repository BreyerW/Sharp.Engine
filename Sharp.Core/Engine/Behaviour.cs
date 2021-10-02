using Sharp.Editor.Views;
using System;

namespace Sharp
{
    public abstract class Behaviour : Component
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

