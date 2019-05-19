using System;
using System.Collections.Generic;

namespace Sharp
{
    public class Light : Component
    {
        internal Color color = Color.White;
        internal float intensity = 1f;
        internal float angle = 90f;

        public static HashSet<Light> lights = new HashSet<Light>();

        //public float radius{ get; set;}=1;
        public LightType type { get; set; } = LightType.Spot;

        public Color Color { get => color; }
        public float Intensity { get => intensity; set => intensity = value; }
        public float Angle { get => angle; set => angle = value; }
        public static float ambientCoefficient = 0.005f;

		public Light(Entity parent) : base(parent)
		{
		}

		protected internal override void OnEnableInternal()
        {
            lights.Add(this);
        }

        protected internal override void OnDisableInternal()
        {
            lights.Remove(this);
        }
    }

    public enum LightType
    {
        Directional,
        Point,
        Spot
    }
}