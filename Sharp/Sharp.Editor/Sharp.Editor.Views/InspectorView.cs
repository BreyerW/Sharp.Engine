using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gwen.Control;
using OpenTK;
using OpenTK.Input;

namespace Sharp.Editor.Views
{
	public class InspectorView:View
	{
		private Func<object> lastInspectedObj=null;
		private PropertyTree ptree;
		public InspectorView ()
		{
			
		}
		public override void Initialize ()
		{
			base.Initialize ();
			ptree=new PropertyTree (canvas);
			ptree.SetBounds(0, 0, 200, 200);
			ptree.ShouldDrawBackground = false;
			Selection.OnSelectionChange += (sender, args) => {
				var entity = (sender as Func<object>)() as Entity;
				if (entity!=null) {
					var comps = entity.GetAllComponents ();
					ptree.RemoveAll ();
					var prop = ptree.Add ("Transform");
					prop.Add ("Position:", new Gwen.Control.Property.Vector3 (prop), entity.Position).ValueChanged += (o, arg) => {
						var tmpObj = o as PropertyRow<Vector3>;
						entity.Position = tmpObj.Value;
					};
					prop.Add ("Rotation:", new Gwen.Control.Property.Vector3 (prop), entity.Rotation).ValueChanged += (o, arg) => {
						var tmpObj = o as PropertyRow<Vector3>;
						entity.Rotation = tmpObj.Value;
					};
					prop.Add ("Scale:", new Gwen.Control.Property.Vector3 (prop), entity.Scale).ValueChanged += (o, arg) => {
						var tmpObj = o as PropertyRow<Vector3>;
						entity.Scale = tmpObj.Value;
					};
					foreach (var component in comps) {
						prop = ptree.Add (component.GetType ().Name);
						var inspector = new DefaultInspector ();
						inspector.properties = prop;
						inspector.getTarget = () => component;
						inspector.OnInitializeGUI ();
					}
					ptree.Show ();
					ptree.ExpandAll ();
				}
				//else
				//props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);

			};
		}
		public override void Render ()
		{
			base.Render ();
			/*if (Selection.assets.Count>0 && Selection.assets.Peek() != lastInspectedObj) {
				lastInspectedObj = Selection.assets.Peek();
			IEnumerable<PropertyInfo> props;

			}*/

		}
		public override void OnResize (int width, int height)
		{
			base.OnResize (width, height);
			ptree.SetBounds(0, 0, canvas.Width, canvas.Height);
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
		public override void OnKeyPressEvent (ref KeyboardState evnt)
		{
		}
	}
}

