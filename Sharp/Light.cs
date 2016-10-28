using System;
using System.Collections.Generic;

namespace Sharp
{
	public class Light : Component
	{
		public static HashSet<Light> lights=new HashSet<Light>();
		//public float radius{ get; set;}=1;
		public LightType type{get; set;}=LightType.Spot;
		public OpenTK.Graphics.Color4 color { get; set;}=OpenTK.Graphics.Color4.White;
		public float intensity{ get; set;}=1f;
        public float angle { get; set; } = 90f;
        public static float ambientCoefficient=0.005f;

		protected internal override void OnEnableInternal ()
		{
			lights.Add (this);
		}
		protected internal override void OnDisableInternal ()
		{
			lights.Remove (this);
		}
	}
	public enum LightType{
		Directional,
		Point,
		Spot
	}
}

