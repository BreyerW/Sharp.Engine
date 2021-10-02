
using Newtonsoft.Json;
using Sharp.Core;
using Sharp.Editor.Attribs;
using Sharp.Engine.Components;
using System;
using System.Text.Json.Serialization;

namespace Sharp
{
    [Serializable]
    public abstract class Component : IEngineObject, IEquatable<Component>
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
        private Entity parent;
        [JsonInclude]
        [NonSerializable]//, JsonProperty
        public Entity Parent
        {
            get => parent;
            private set
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

        public virtual void Dispose()
        {
            active = false;
            Extension.entities.RemoveEngineObject(this);
            //Extension.objectToIdMapping.Remove(this);
            PluginManager.serializer.objToIdMapping.Remove(this);
        }

        public bool Equals(Component other)
        {
            return ReferenceEquals(this, other);
        }

        public Component(Entity parent)
        {
            Parent = parent;
            Extension.entities.AddEngineObject(this);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Component);
        }
    }
}