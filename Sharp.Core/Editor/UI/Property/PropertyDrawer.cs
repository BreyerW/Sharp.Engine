using BepuUtilities.Memory;
using FastMember;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Sharp.Core.Editor;
using Sharp.Editor.Attribs;
using Sharp.Editor.Views;
using Sharp.Engine.Components;
using SharpAsset;
using SharpAsset.Pipeline;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Editor.UI.Property
{
	public abstract class PropertyDrawer : Control
	{
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
		public MemberInfo memberInfo;
		public CustomPropertyDrawerAttribute[] attributes;
		private Component target;
		protected Label label = new Label();
		protected Type propertyType;
		//private static Microsoft.IO.RecyclableMemoryStreamManager memStream = new Microsoft.IO.RecyclableMemoryStreamManager();
		private RefFunc<object, T> getter;
		//private RefAction<object, T> setter;

		public override Component Target
		{
			set
			{
				if (target == value) return;
				target = value;
			}
			get => target;
		}
		public ref T Value => ref getter(target);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int DangerousGetObjectDataByteOffset(object obj, FieldInfo field)
		{
			var rawObj = Unsafe.As<object, IntPtr>(ref obj);

			return rawObj.ToInt32() - field.FieldHandle.Value.ToInt32();
		}

		public PropertyDrawer(MemberInfo memInfo)
		{
			memberInfo = memInfo;
			(getter, _) = DelegateGenerator.GetAccessors<T>(memberInfo);
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