using System;
using System.Collections.Generic;

namespace Sharp
{
	public class Light : Component
	{
		public static HashSet<Light> lights=new HashSet<Light>();
		//public float radius{ get; set;}=1;
		public LightType type{get; set;}=LightType.Directional;
		public OpenTK.Graphics.Color4 color { get; set;}=OpenTK.Graphics.Color4.White;

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

