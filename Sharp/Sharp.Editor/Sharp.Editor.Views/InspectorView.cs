using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gwen.Control;
using OpenTK;
using OpenTK.Input;
using Sharp.Editor.UI;

namespace Sharp.Editor.Views
{
    public class InspectorView : View
    {
        private PropertyTree ptree;
        private MenuStrip tagStrip;

        public InspectorView()
        {

        }
        public override void Initialize()
        {
            base.Initialize();
            ptree = new PropertyTree(panel);
            tagStrip = new MenuStrip(panel);
            var root = tagStrip.AddItem("Add tag");
            root.Position(Gwen.Pos.Top, 0, 100);
            root.Menu.Position(Gwen.Pos.Top, 0, 20);
            root.Clicked += (sender, arguments) => { var menu = sender as MenuItem; menu.Menu.Show(); };
            foreach (var tag in TagsContainer.allTags)
                root.Menu.AddItem(tag);
            root.Menu.AddDivider();
            root.Menu.AddItem("Create new tag").SetAction((Base sender, EventArgs arguments) => Console.WriteLine());
            //root.Menu.;
            tagStrip.Hide();
            ptree.ShouldDrawBackground = false;
            Selection.OnSelectionChange += (sender, args) =>
            {
                Console.WriteLine("SelectionChange");
                ptree.RemoveAll();
                if (sender is Entity entity) RenderComponents(entity);
                //else
                //props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);

                ptree.Show();
                ptree.SetBounds(0, 25, panel.Width, 200);
                ptree.ExpandAll();
            };
            Selection.OnSelectionDirty += (sender, args) =>
            {
                if (sender is Entity entity) RenderComponents(entity);
            };
        }
        public void RenderComponents(Entity entity)
        {
            Console.WriteLine("renderComps");
            var prop = ptree.AddOrGet("Transform");
            DefaultInspector.mappedPropertyDrawers[typeof(OpenTK.Vector3)].Invoke(prop, "Position", entity.Position, (object val) => { entity.Position = (Vector3)val; });
            DefaultInspector.mappedPropertyDrawers[typeof(OpenTK.Vector3)].Invoke(prop, "Rotation", entity.Rotation, (object val) => { entity.Rotation = (Vector3)val; });
            DefaultInspector.mappedPropertyDrawers[typeof(OpenTK.Vector3)].Invoke(prop, "Scale", entity.Scale, (object val) => { entity.Scale = (Vector3)val; });

            var comps = entity.GetAllComponents();
            foreach (var component in comps)
            {
                prop = ptree.AddOrGet(component.GetType().Name);
                var inspector = new DefaultInspector();
                inspector.properties = prop;
                inspector.getTarget = component;
                inspector.OnInitializeGUI();
            }
            tagStrip.Show();
        }
        public override void Render()
        {
            //base.Render ();
            /*if (Selection.assets.Count>0 && Selection.assets.Peek() != lastInspectedObj) {
				lastInspectedObj = Selection.assets.Peek();
			IEnumerable<PropertyInfo> props;

			}*/

        }
        public override void OnResize(int width, int height)
        {
            base.OnResize(width, height);
            ptree.SetBounds(0, 25, panel.Width, height);
        }
        Action<object> CreateSetter(object instance, MethodInfo propMethod)
        {

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
        Func<object> CreateGetter(object instance, MethodInfo propMethod)
        {

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

        public override void OnKeyPressEvent(ref KeyboardState evnt)
        {
        }
    }
    public class DelegateBuilder
    {
        public static T BuildDelegate<T>(MethodInfo method, params object[] missingParamValues)
        {
            var queueMissingParams = new Queue<object>(missingParamValues);

            var dgtMi = typeof(T).GetMethod("Invoke");
            var dgtRet = dgtMi.ReturnType;
            var dgtParams = dgtMi.GetParameters();

            var paramsOfDelegate = dgtParams
                .Select(tp => Expression.Parameter(tp.ParameterType, tp.Name))
                .ToArray();

            var methodParams = method.GetParameters();

            if (method.IsStatic)
            {
                var paramsToPass = methodParams
                    .Select((p, i) => CreateParam(paramsOfDelegate, i, p, queueMissingParams))
                    .ToArray();

                var expr = Expression.Lambda<T>(
                    Expression.Call(method, paramsToPass),
                    paramsOfDelegate);

                return expr.Compile();
            }
            else
            {
                var paramThis = Expression.Convert(paramsOfDelegate[0], method.DeclaringType);

                var paramsToPass = methodParams
                    .Select((p, i) => CreateParam(paramsOfDelegate, i + 1, p, queueMissingParams))
                    .ToArray();

                var expr = Expression.Lambda<T>(
                    Expression.Call(paramThis, method, paramsToPass),
                    paramsOfDelegate);

                return expr.Compile();
            }
        }

        private static Expression CreateParam(ParameterExpression[] paramsOfDelegate, int i, ParameterInfo callParamType, Queue<object> queueMissingParams)
        {
            if (i < paramsOfDelegate.Length)
                return Expression.Convert(paramsOfDelegate[i], callParamType.ParameterType);

            if (queueMissingParams.Count > 0)
                return Expression.Constant(queueMissingParams.Dequeue());

            if (callParamType.ParameterType.IsValueType)
                return Expression.Constant(Activator.CreateInstance(callParamType.ParameterType));

            return Expression.Constant(null);
        }
    }
}

