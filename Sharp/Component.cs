using Sharp.Editor.Attribs;
using System;

namespace Sharp
{
	[Serializable]
	public abstract class Component : IEngineObject
	{
		[NonSerializable]
		public Guid Id { get; internal set; } = Guid.NewGuid();

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
		} = 1;

		public string testStr
		{
			get;
			set;
		} = "test";

		// public float[,,] testArray
		// {
		//    get;
		//    set;
		//  } = { { { 80, 45 }, { 80, 45 }, { 80, 45 } } };
		//[CurveRange()]
		public Curve[] curves { get; set; } = new Curve[2] {
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.1f, value = -10f }, new Keyframe() { time = 120f, value = 10f } } },
			new Curve() { keys = new Keyframe[] { new Keyframe() { time = 0.4f, value = 0f }, new Keyframe() { time = 60f, value = 1f } } }
		};

		public Entity entityObject;

		protected internal virtual void OnEnableInternal()
		{
		}

		protected internal virtual void OnDisableInternal()
		{
		}

		public void Destroy()
		{
			Editor.Views.SceneView.entities.RemoveEngineObject(this);
		}

		public Component()
		{
			active = true;
			Editor.Views.SceneView.entities.AddEngineObject(this);
			//id = ++Entity.lastId;
			// Entity.lastId = id;
		}
	}
}