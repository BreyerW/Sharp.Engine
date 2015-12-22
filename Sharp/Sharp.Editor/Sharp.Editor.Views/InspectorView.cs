using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gwen.Control;

namespace Sharp.Editor.Views
{
	public class InspectorView:View
	{
		private object lastInspectedObj=null;
		private PropertyTree ptree;
		public InspectorView ()
		{
			
		}
		public override void Initialize ()
		{
			base.Initialize ();
			ptree=new PropertyTree (canvas);
			ptree.SetBounds(0, 0, 200, 200);
		}
		public override void Render ()
		{
			base.Render ();
			if (Selection.assets.Count>0 && Selection.assets[0] != lastInspectedObj) {
				lastInspectedObj = Selection.assets [0];
			IEnumerable<PropertyInfo> props;
			if (Selection.assets [0] is Entity) {
				var comps= (Selection.assets [0] as Entity).GetAllComponents ();
					ptree.RemoveAll ();
				foreach(var component in comps){
						var prop=ptree.Add (component.GetType().ToString());
					/*props=component.GetType ().GetProperties ().Where (p => p.CanRead && p.CanWrite);
					foreach(var prop in props){
						prop.GetMethod.CreateDelegate(typeof(Func<object>),Selection.assets [0]);
						prop.SetMethod.CreateDelegate(typeof(Action<object>),Selection.assets [0]);
					}*/
				}
					ptree.Show();
					ptree.ExpandAll ();
			}
				else
					props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);
			}

		}
		public override void OnResize (int width, int height)
		{
			base.OnResize (width, height);
			ptree.SetBounds(0, 0, width, height);
		}
		Action<object> CreateSetter(object instance, MethodInfo propMethod){

			// Create a parameter for the method call expression.
			ParameterExpression param = Expression.Parameter(propMethod.DeclaringType, "val");

			// Create a method call expression.
			// The expression will be .call <instance>.set_Name(val), where val is the parameter to the method.
			MethodCallExpression call = Expression.Call(Expression.Constant(instance), propMethod,
				new ParameterExpression[] { param });

			// Create a delegate whose implementation is the expression we just created.
			// An Action is a delegate that takes a single parameter and returns void.
			// Create a Lambda expression whose body is the MethodCallExpression that takes a single parameter.
			// The parameter will be passed at run time.
			return Expression.Lambda<Action<object>>(call, param).Compile();
		}
		Func<object> CreateGetter(object instance, MethodInfo propMethod){

			// Create a parameter for the method call expression.
			ParameterExpression param = Expression.Parameter(typeof(object), "val");

			// Create a method call expression.
			// The expression will be .call <instance>.set_Name(val), where val is the parameter to the method.
			MethodCallExpression call = Expression.Call(Expression.Constant(instance), propMethod,
				new ParameterExpression[] { param });

			// Create a delegate whose implementation is the expression we just created.
			// An Action is a delegate that takes a single parameter and returns void.
			// Create a Lambda expression whose body is the MethodCallExpression that takes a single parameter.
			// The parameter will be passed at run time.
			return Expression.Lambda<Func<object>>(call, param).Compile();
		}
	}
}

