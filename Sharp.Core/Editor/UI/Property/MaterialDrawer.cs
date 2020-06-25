using Sharp.Editor.UI.Property;
using SharpAsset;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace Sharp.Core.Editor.UI.Property
{
	 class MaterialDrawer : PropertyDrawer<Material>
	{
		//private static MaterialComparer equality = new MaterialComparer();
		//private Material mat = new Material();
		//public override IEqualityComparer<Material> Equality =>equality;

		public MaterialDrawer(MemberInfo memInfo) : base(memInfo)
		{
		}

	/*	public override Material Value { get => mat; 
			set {
				mat.Shader = value.Shader;
				mat.localParams = value.localParams;
			}
		}*/
	}
	class MaterialComparer : IEqualityComparer<Material>
	{
		public bool Equals([AllowNull] Material x, [AllowNull] Material y)
		{
			if (x is null || y is null) return false;
			return x.Shader.Program == y.Shader.Program && x.localParams == y.localParams;
		}

		public int GetHashCode([DisallowNull] Material obj)
		{
			throw new NotImplementedException();
		}
	}
}
