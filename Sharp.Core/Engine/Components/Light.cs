using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Sharp
{
	public class Light : Component//TODO: renderer?
	{
		[ModuleInitializer]
		public static void Register()
		{
			Extension.RegisterComponent<Light>();
		}
		internal Color color = Color.White;
		internal float intensity = 1f;
		public float angle = 90f;

		public static HashSet<Light> lights = new HashSet<Light>();

		//public float radius{ get; set;}=1;
		public LightType type { get; set; } = LightType.Spot;

		public Color Color { get => color; }
		public float Intensity { get => intensity; set => intensity = value; }
		public float Angle { get => angle; set => angle = value; }
		public static float ambientCoefficient = 0.005f;

		internal override void OnActiveChanged()
		{
			if (active)
				lights.Add(this);
			else
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