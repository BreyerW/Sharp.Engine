using Newtonsoft.Json;
using Sharp.Editor.Attribs;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Sharp
{
	[Serializable]
	public abstract class Component : IEngineObject, IEquatable<Component>
	{
		public bool enabled;

		public bool active
		{
			get { return enabled; }
			set
			{
				if (enabled == value)
					return;
				Console.WriteLine("disable");
				enabled = value;
				OnActiveChanged();
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

		internal virtual void OnActiveChanged()
		{

		}

		public virtual void Dispose()
		{
			active = false;
			Extension.entities.RemoveEngineObject(this);
			Extension.objectToIdMapping.Remove(this);
		}

		public bool Equals(Component other)
		{
			return ReferenceEquals(this, other);
		}

		public Component()
		{
			Extension.entities.AddEngineObject(this);
		}
	}
}