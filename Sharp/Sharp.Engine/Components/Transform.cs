using System;
using System.Numerics;

namespace Sharp.Engine.Components
{
	public class Transform : Component//1 ui szablon dla roznych typow w inspectorze
	{
		public Vector3 Position
		{
			get
			{
				return entityObject.position;
			}
			set
			{
				entityObject.position = value;
				entityObject.onTransformChanged?.Invoke();
			}
		}

		public Vector3 Rotation
		{
			get
			{
				return entityObject.rotation;
			}
			set
			{
				entityObject.rotation = value;
				entityObject.onTransformChanged?.Invoke();
			}
		}

		public Vector3 Scale
		{
			get
			{
				return entityObject.scale;
			}
			set
			{
				entityObject.scale = value;
				entityObject.onTransformChanged?.Invoke();
			}
		}
	}
}