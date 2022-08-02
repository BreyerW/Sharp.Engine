
using Newtonsoft.Json;
using Sharp.Core;
using Sharp.Editor.Attribs;
using Sharp.Engine.Components;
using System;
using System.Text.Json.Serialization;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace Sharp
{
	[Serializable]
	public abstract class Component : IEngineObject/*, IEquatable<Component>*/
	{
		//[JsonInclude]
		private bool enabled;
		//[JsonIgnore]
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
		[JsonInclude]
		private Entity parent;
		[JsonIgnore]
		//[NonSerializable]//, JsonProperty
		public Entity Parent
		{
			get => parent;
			set
			{
				if (this is Transform t)
					value.transform = t;
				if (parent == value) return;
				value.components.Add(this);
				//value.ComponentsMask = value.ComponentsMask.SetTag(this);
				parent = value;
			}
		}
		internal virtual void OnActiveChanged()
		{

		}
		protected virtual void Initialize()
		{
		}
		internal void InternalInitialize()
		{
			Extension.entities.AddEngineObject(this);
			Initialize();
		}

		public virtual void Dispose()
		{
			active = false;
			Extension.entities.RemoveEngineObject(this);
			//Extension.objectToIdMapping.Remove(this);
			PluginManager.serializer.objToIdMapping.Remove(this);
		}

		/*public bool Equals(Component other)
		{
			return ReferenceEquals(this, other);
		}*/


		/*public override bool Equals(object obj)
		{
			return Equals(obj as Component);
		}*/
	}
}