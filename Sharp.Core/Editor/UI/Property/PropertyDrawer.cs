﻿using Squid;
using System;
using System.Reflection;
using Microsoft.Toolkit.HighPerformance.Extensions;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
		public abstract Component Target
		{
			set;
			get;
		}
		public virtual bool CanApply(MemberInfo memInfo) => true;//var attribs = propertyInfo.GetCustomAttributes<CustomPropertyDrawerAttribute>(true);
	}

	/// <summary>
	/// Base control for property entry.
	/// </summary>
	public abstract class PropertyDrawer<T> : PropertyDrawer
	{
		public MemberInfo memberInfo;
		private Component target;
		protected Label label = new Label();
		protected Type propertyType;
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
		public ref T Value => ref target.DangerousGetObjectDataReferenceAt<T>(offset);

		public PropertyDrawer(MemberInfo memInfo)
		{
			memberInfo = memInfo;
			offset = (memberInfo as FieldInfo).GetFieldOffset();
			//Scissor = true;
			//Size = new Point(0, 20);
			Name = memberInfo.Name;
			propertyType = memberInfo.GetUnderlyingType();

			propertyType = propertyType.IsByRef ? propertyType.GetElementType() : propertyType;
			label.Text = memberInfo.Name + ": ";
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