using Sharp.Editor.Attribs;
using System;

namespace Sharp
{
	[Serializable]
	public abstract class Component : IEngineObject
	{
		private bool enabled;

		public bool active
		{
			get { return enabled; }
			set
			{
				if (enabled == value)
					return;
				Console.WriteLine("disable");
				enabled = value;
				if (enabled)
					OnEnableInternal();
				else
					OnDisableInternal();
			}
		}

		[Range(0, 100)]
		public float testRange
		{
			get;
			set;
		} //= 1;

		// public float[,,] testArray
		// {
		//    get;
		//    set;
		//  } = { { { 80, 45 }, { 80, 45 }, { 80, 45 } } };
		//[CurveRange()]
		
		[NonSerializable]
		public Entity Parent
		{
			get;
			internal set;
		}

		protected internal virtual void OnEnableInternal()
		{
		}

		protected internal virtual void OnDisableInternal()
		{
		}

		public void Destroy()
		{
			active = false;
			Extension.entities.RemoveEngineObject(this);
		}
		public Component(Entity parent)
		{
			if (parent is null) throw new ArgumentNullException("You tried creating component without attaching it to entity");
			Parent = parent;
			active = true;
			Extension.entities.AddEngineObject(this);
		}
	}
}