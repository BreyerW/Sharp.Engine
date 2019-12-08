using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Sharp
{
	public delegate void RefAction<T1, T2>(T1 instance, ref T2 value);
	internal delegate ref TResult RefFunc<T, TResult>(T instance);
	internal static class DelegateGenerator
	{
		internal static Dictionary<(Type declaringType, string member), (Delegate getter, Delegate setter)> accessorsMapping = new Dictionary<(Type declaringType, string member), (Delegate getter, Delegate setter)>();

		public static (Func<object, T> getter, RefAction<object, T> setter) GetAccessors<T>(MemberInfo memberInfo)
		{
			if (accessorsMapping.TryGetValue((memberInfo.DeclaringType, memberInfo.Name), out var accessors))
				return (accessors.getter as Func<object, T>, accessors.setter as RefAction<object, T>);
			var getter = GenerateGetter<T>(memberInfo);
			var setter = GenerateSetter<T>(memberInfo);
			accessorsMapping.Add((memberInfo.DeclaringType, memberInfo.Name), (getter, setter));
			return (getter, setter);
		}
		/// <summary>
		/// Generate open setter for field or property
		/// </summary>
		/// <typeparam name="T">Type of unbound instance</typeparam>
		/// <param name="memberInfo">Field or property info</param>
		/// <returns></returns>
		public static RefAction<object, T> GenerateSetter<T>(MemberInfo memberInfo)//TODO: inject event calls?
		{
			//throw new NotSupportedException("Assign for ref getters is bugged with S.L.Expressions (PropertyInfo.CanWrite is set false for ref getters which is wrong)");
			//parameter "target", the object on which to set the field `field`
			if (memberInfo.GetUnderlyingType() is { IsByRef: true })
			{
				var method = new DynamicMethod("", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, null, new[] { typeof(object), typeof(T).MakeByRefType() }, memberInfo.DeclaringType, true);
				var il = method.GetILGenerator();
				var get = (memberInfo as PropertyInfo).GetGetMethod();
				var memType = memberInfo.GetUnderlyingType().GetElementType();
				il.DeclareLocal(memberInfo.DeclaringType);
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, memberInfo.DeclaringType);
				il.Emit(OpCodes.Callvirt, get);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldind_Ref);
				if (memType.IsClass)
				{
					if (typeof(T) == typeof(object))
						il.Emit(OpCodes.Castclass, memType);
					il.Emit(OpCodes.Stind_Ref);
				}
				else
				{
					if (typeof(T) == typeof(object))
						il.Emit(OpCodes.Unbox_Any, memType);
					il.Emit(OpCodes.Stobj, memType);
				}
				il.Emit(OpCodes.Ret);
				return method.CreateDelegate(typeof(RefAction<object, T>)) as RefAction<object, T>;
			}
			ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
			(var assignExp, var valueExp) = SetterHelper<T>(targetExp, memberInfo);

			//compile the whole thing
			return Expression.Lambda<RefAction<object, T>>(assignExp, targetExp, valueExp).Compile();
		}
		/// <summary>
		/// Generate open getter for field or property
		/// </summary>
		/// <typeparam name="T">Type of unbound instance</typeparam>
		/// <param name="memberInfo">Field or property info</param>
		/// <returns></returns>
		public static Func<object, T> GenerateGetter<T>(MemberInfo memberInfo)
		{
			if (memberInfo.GetUnderlyingType() is { IsByRef: true })
			{
				var method = new DynamicMethod("", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(T), new[] { typeof(object) }, memberInfo.DeclaringType, true);
				var il = method.GetILGenerator();
				var get = (memberInfo as PropertyInfo).GetGetMethod();
				var type = memberInfo.GetUnderlyingType().GetElementType();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, memberInfo.DeclaringType);
				il.Emit(OpCodes.Callvirt, get);
				if (type.IsClass)
				{
					il.Emit(OpCodes.Ldind_Ref);
				}
				else
				{
					il.Emit(OpCodes.Ldobj, type);
					if (typeof(T) == typeof(object))
						il.Emit(OpCodes.Box, type);
				}
				il.Emit(OpCodes.Ret);
				return method.CreateDelegate(typeof(Func<object, T>)) as Func<object, T>;
			}
			ParameterExpression paramExpression = Expression.Parameter(typeof(object), "value");
			var castValueExp = CreateCastExpression(paramExpression, memberInfo.DeclaringType);
			Expression memberExp = CreateMemberExpression(castValueExp, memberInfo);

			if (typeof(T) != memberInfo.GetUnderlyingType())
				memberExp = CreateCastExpression(memberExp, typeof(T));
			return Expression.Lambda<Func<object, T>>(memberExp, paramExpression).Compile();
		}

		/// <summary>
		/// Generate closed setter for field or property
		/// </summary>
		/// <param name="instance">Instance to which setter will be bound</param>
		/// <param name="memberInfo">Field or property info</param>
		/// <returns></returns>
		public static Action<T> GenerateSetter<T>(object instance, MemberInfo memberInfo)
		{
			//parameter "target", the object on which to set the field `field`
			ConstantExpression targetExp = Expression.Constant(instance);

			(var assignExp, var valueExp) = SetterHelper<T>(targetExp, memberInfo);

			//compile the whole thing
			return Expression.Lambda<Action<T>>(assignExp, valueExp).Compile();
		}

		/// <summary>
		/// Generate closed getter for field or property
		/// </summary>
		/// <param name="instance">Instance to which setter will be bound</param>
		/// <param name="memberInfo">Field or property info</param>
		/// <returns></returns>
		public static Func<T> GenerateGetter<T>(object instance, MemberInfo memberInfo)
		{
			ConstantExpression paramExpression = Expression.Constant(instance);

			MemberExpression memberExp = CreateMemberExpression(paramExpression, memberInfo);

			Expression castValueExp = memberExp;
			//cast the value to its correct type
			if (typeof(T) != memberInfo.GetUnderlyingType())
				castValueExp = CreateCastExpression(memberExp, typeof(T));

			return Expression.Lambda<Func<T>>(castValueExp).Compile();
		}


		private static (Expression assign, ParameterExpression param) SetterHelper<T>(Expression targetExp, MemberInfo memberInfo)
		{
			var reqValType = typeof(T);
			//parameter "value" the value to be set in the `field` on "target"
			ParameterExpression valueExp = Expression.Parameter(reqValType.MakeByRefType(), "value");

			//cast the target from object to its correct type
			Expression castTartgetExp = CreateCastExpression(targetExp, memberInfo.DeclaringType);

			Expression castValueExp = valueExp;
			//cast the value to its correct type
			var memberType = memberInfo.GetUnderlyingType();
			if (reqValType != (memberType.IsByRef ? memberType.GetElementType() : memberType))
				castValueExp = CreateCastExpression(valueExp, memberType);

			//the field `field` on "target"
			MemberExpression memberExp = CreateMemberExpression(castTartgetExp, memberInfo);

			//assign the "value" to the `field`
			Expression assignExp = Expression.Assign(memberExp, castValueExp);
			return (assignExp, valueExp);
		}

		private static Expression CreateCastExpression(Expression exp, Type type)
		{
			return type.IsValueType
				? Expression.Unbox(exp, type)
				: Expression.Convert(exp, type);
		}

		private static MemberExpression CreateMemberExpression(Expression exp, MemberInfo memberInfo)
		{
			return memberInfo is FieldInfo fieldInfo ? Expression.Field(exp, fieldInfo)
				: memberInfo is PropertyInfo propertyInfo ? Expression.Property(exp, propertyInfo)
				: null;//method call/events unsupported
		}

		private static MemberExpression CreateRefMemberExpression(Expression exp, MemberInfo memberInfo)
		{
			return memberInfo is FieldInfo fieldInfo ? Expression.Field(exp, fieldInfo.FieldType.MakeByRefType(), fieldInfo.Name)
				: memberInfo is PropertyInfo propertyInfo ? Expression.Property(exp, propertyInfo.PropertyType.MakeByRefType(), propertyInfo.Name)
				: null;//method call/events unsupported
		}

		public static Action<TInstance, T1> RewriteGetterAsSetter<T1, TInstance>(Expression<Func<TInstance, T1>> getter)
		{
			var member = (MemberExpression)getter.Body;
			var param = Expression.Parameter(typeof(T1), "value");
			var setter = Expression.Lambda<Action<TInstance, T1>>(
				Expression.Assign(member, param), getter.Parameters[0], param);

			// compile it
			return setter.Compile();
		}

		public static Action<T1> RewriteGetterAsSetter<T1>(Expression<Func<T1>> getter)//nie ma potrzeby robienia expression.constant(instance)
		{
			var member = (MemberExpression)getter.Body;
			var param = Expression.Parameter(typeof(T1), "value");
			var setter = Expression.Lambda<Action<T1>>(
				Expression.Assign(member, param), getter.Parameters[0], param);

			// compile it
			return setter.Compile();
		}
	}

	internal static class DelegateGeneratorEx
	{
		public static Type GetUnderlyingType(this MemberInfo member)
		{
			switch (member.MemberType)
			{
				case MemberTypes.Event:
					return ((EventInfo)member).EventHandlerType;

				case MemberTypes.Field:
					return ((FieldInfo)member).FieldType;

				case MemberTypes.Method:
					return ((MethodInfo)member).ReturnType;

				case MemberTypes.Property:
					return ((PropertyInfo)member).PropertyType;

				default:
					throw new ArgumentException
					(
					 "Input MemberInfo must be if type EventInfo, FieldInfo, MethodInfo, or PropertyInfo"
					);
			}
		}
	}
}