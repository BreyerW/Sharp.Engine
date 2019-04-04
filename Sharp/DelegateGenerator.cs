using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Sharp
{
    internal static class DelegateGenerator
    {
        /// <summary>
        /// Generate open setter for field or property
        /// </summary>
        /// <typeparam name="T">Type of unbound instance</typeparam>
        /// <param name="memberInfo">Field or property info</param>
        /// <returns></returns>
        public static Action<object, T> GenerateSetter<T>(MemberInfo memberInfo)//TODO: inject event calls?
        {
            //parameter "target", the object on which to set the field `field`
            ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");
            var castValueExp = CreateCastExpression(targetExp, memberInfo.DeclaringType);
            (var assignExp, var valueExp) = SetterHelper<T>(castValueExp, memberInfo);

            //compile the whole thing
            return Expression.Lambda<Action<object, T>>(assignExp, targetExp, valueExp).Compile();
        }

        /// <summary>
        /// Generate open getter for field or property
        /// </summary>
        /// <typeparam name="T">Type of unbound instance</typeparam>
        /// <param name="memberInfo">Field or property info</param>
        /// <returns></returns>
        public static Func<object, T> GenerateGetter<T>(MemberInfo memberInfo)
        {
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

        /* public static Action<object, T> GenerateNestedSetter<T>(string nestedPropName, params int[] indexes)
         {
         }

         public static Func<object, T> GenerateNestedGetter<T>(string nestedPropName, params int[] indexes)
         {
         }*/

        private static (BinaryExpression assign, ParameterExpression param) SetterHelper<T>(Expression targetExp, MemberInfo memberInfo)
        {
            //parameter "value" the value to be set in the `field` on "target"
            ParameterExpression valueExp = Expression.Parameter(typeof(T), "value");

            //cast the target from object to its correct type
            Expression castTartgetExp = CreateCastExpression(targetExp, memberInfo.DeclaringType);

            Expression castValueExp = valueExp;
            //cast the value to its correct type
            var memberType = memberInfo.GetUnderlyingType();
            if (typeof(T) != memberType)
                castValueExp = CreateCastExpression(valueExp, memberType);

            //the field `field` on "target"
            MemberExpression memberExp = CreateMemberExpression(castTartgetExp, memberInfo);

            //assign the "value" to the `field`
            BinaryExpression assignExp = Expression.Assign(memberExp, castValueExp);
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

    //some experimental code from Reflection.cs in BJSON
    /*  internal static GenericSetter CreateSetField(Type type, FieldInfo fieldInfo)
        {
            return GenerateSetter(type, fieldInfo);
        }

        internal static GenericSetter CreateSetMethod(Type type, PropertyInfo propertyInfo)
        {
            return GenerateSetter(type, propertyInfo);
        }

        internal static GenericGetter CreateGetField(Type type, FieldInfo fieldInfo)
        {
            return GenerateGetter(type, fieldInfo);
        }

        internal static GenericGetter CreateGetMethod(Type type, PropertyInfo propertyInfo)
        {
            return GenerateGetter(type, propertyInfo);
        }

        /// <summary>
        /// Generate open setter for field or property
        /// </summary>
        /// <typeparam name="T">Type of unbound instance</typeparam>
        /// <param name="memberInfo">Field or property info</param>
        /// <returns></returns>
        public static GenericSetter GenerateSetter(Type type, MemberInfo memberInfo)//TODO: inject event calls?
        {
            if(memberInfo is FieldInfo f && f.IsInitOnly)
                return null;
            //parameter "target", the object on which to set the field `field`
            ParameterExpression targetExp = Expression.Parameter(typeof(object), "target");

            (var assignExp, var valueExp) = SetterHelper(type, targetExp, memberInfo);

            //compile the whole thing
            return Expression.Lambda<GenericSetter>(assignExp, targetExp, valueExp).Compile();
        }

        /// <summary>
        /// Generate open getter for field or property
        /// </summary>
        /// <typeparam name="T">Type of unbound instance</typeparam>
        /// <param name="memberInfo">Field or property info</param>
        /// <returns></returns>
        public static GenericGetter GenerateGetter(Type type, MemberInfo memberInfo)
        {
            ParameterExpression paramExpression = Expression.Parameter(typeof(object), "value");

            UnaryExpression memberExp = (UnaryExpression)CreateCastExpression(CreateMemberExpression(CreateCastExpression(paramExpression, type), memberInfo), typeof(object));

            return Expression.Lambda<GenericGetter>(memberExp, paramExpression).Compile();
        }

        private static (Expression assign, ParameterExpression param) SetterHelper(Type type, Expression targetExp, MemberInfo memberInfo)
        {
            var memberType = memberInfo.GetUnderlyingType();
            //parameter "value" the value to be set in the `field` on "target"
            ParameterExpression valueExp = Expression.Parameter(typeof(object), "value");

            //cast the target from object to its correct type
            Expression castTartgetExp = CreateCastExpression(targetExp, memberInfo.DeclaringType);

            Expression castValueExp = valueExp;
            //cast the value to its correct type

            //if (type != memberType)
            castValueExp = CreateCastExpression(valueExp, memberType);
            //the field `field` on "target"
            MemberExpression memberExp = CreateMemberExpression(castTartgetExp, memberInfo);

            //assign the "value" to the `field`
            var assignExp = CreateCastExpression(Expression.Assign(memberExp, castValueExp), typeof(object));
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
        }*/
}