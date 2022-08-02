using Microsoft.Toolkit.HighPerformance.Extensions;
using Squid;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		internal Label label = new();
		internal Action<object, object> setter;
		internal Func<object, object> getter;
		public abstract Component Target
		{
			set;
			get;
		}
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer
	{

		//public MemberInfo memberInfo;
		private Component target;

		//protected Type propertyType;
		private IntPtr offset;
		public override Component Target
		{
			set
			{
				if (target == value) return;
				target = value;
			}
			get => target;
		}
		public object Value
		{
			set => setter(target, value);
			get => getter(target);
		} //ref target.DangerousGetObjectDataReferenceAt<T>(offset);
		/*public ref T Value
		{
			get
			{
				ref byte addr = ref Unsafe.As<byte[]>(target)[0];
				addr = Unsafe.AddByteOffset(ref addr, offset);
				return ref Unsafe.As<byte, T>(ref addr);
			}
		}*/


		public PropertyDrawer()
		{
			label.Size = new Point(75, Size.y);
			label.AutoEllipsis = false;
			Childs.Add(label);
		}
		//public abstract bool IsValid(CustomPropertyDrawerAttribute[] attributes);
	}

	public static class TypeExtensions
	{
		public static bool IsSubclassOfOpenGeneric(this Type toCheck, Type type)
		{
			if (toCheck.IsAbstract) return false;
			while (toCheck != null && toCheck != typeof(object) && toCheck != type)
			{
				var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
				if (type == cur)
				{
					return true;
				}
				toCheck = toCheck.BaseType;
			}
			return false;
		}
	}
}