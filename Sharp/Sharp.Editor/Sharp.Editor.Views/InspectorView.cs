using System;
using System.Linq;
using System.Reflection;
using Sharp.Editor.UI.Property;
using Squid;
using Sharp.Editor.UI;
using Sharp.Editor.Attribs;
using System.Collections;
using System.Collections.Generic;

namespace Sharp.Editor.Views
{
    public class InspectorView : View
    {
        protected override string Name => "Inspector";

        private TreeView ptree = new TreeView();

        //private ListBox tagStrip = new ListBox();
        private DropDownButton tagStrip = new DropDownButton();

        internal static Dictionary<Type, (Type type, MethodInfo method)> mappedPropertyDrawers = new Dictionary<Type, (Type type, MethodInfo method)>();

        static InspectorView()
        {
            //var primitiveResult = Assembly.GetExecutingAssembly()
            //   .GetTypes()
            // .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
            //      t.BaseType.GetGenericTypeDefinition() == typeof(Gwen.Control.Property.PropertyDrawer<>));//current solution does not support color case

            var result = Assembly.GetExecutingAssembly().GetTypes()
             .Where(t => t.IsSubclassOfOpenGeneric(typeof(PropertyDrawer<>)));

            var method = typeof(InspectorView).GetMethod("Add");
            Type[] genericArgs;
            MethodInfo genericMethod;

            foreach (var type in result)
            {
                genericArgs = type.BaseType.GetGenericArguments();
                genericMethod = method.MakeGenericMethod(new[] { genericArgs[0] });
                mappedPropertyDrawers.Add(genericArgs[0], (type, genericMethod));
                Console.WriteLine("GENERATE FACTORY " + genericMethod);
            }
        }

        public InspectorView(uint attachToWindow) : base(attachToWindow)
        {
            tagStrip.Size = new Point(0, 25);
            tagStrip.Dock = DockStyle.FillX;
            tagStrip.Text = "Tags";
            tagStrip.Style = "label";
            tagStrip.Dropdown.Style = "";
            tagStrip.Parent = panel;

            var button = new Button();
            button.Text = "Add tag";
            button.Parent = tagStrip.Dropdown;
            button.Dock = DockStyle.FillX;
            button.TextAlign = Alignment.MiddleLeft;

            ptree.Dock = DockStyle.Fill;
            ptree.Margin = new Margin(0, 25, 0, 0);
            ptree.Parent = panel;
            ptree.Scrollbar.Size = new Point(0, 0);
            ptree.Scissor = false;
            Selection.OnSelectionChange += (sender, args) =>
            {
                Console.WriteLine("SelectionChange");
                ptree.Nodes.Clear();
                if (sender is Entity entity) RenderComponents(entity);
                //else
                //props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);

                //ptree.ExpandAll();
            };
            Selection.OnSelectionDirty += (sender, args) =>
            {
                //if (sender is Entity entity) RenderComponents(entity);
            };
        }

        /* tagStrip = new MenuStrip(panel);
             ptree = new PropertyTree(panel);
             tagStrip.Dock = Gwen.Pos.Top;
             tagStrip.Margin = new Gwen.Margin(0, 1, 0, 0);
             var root = tagStrip.AddItem("Add tag");
             root.Dock = Gwen.Pos.Center;
             root.Clicked += (sender, arguments) => { var menu = sender as MenuItem; menu.Menu.Show(); };
             foreach (var tag in TagsContainer.allTags)
                 root.Menu.AddItem(tag);
             root.Menu.AddDivider();
             root.Menu.AddItem("Create new tag").SetAction((Base sender, EventArgs arguments) => Console.WriteLine());
             //root.Menu.;
             tagStrip.Hide();
             ptree.ShouldDrawBackground = false;
             ptree.Dock = Gwen.Pos.Fill;

             base.Initialize();*/

        public override void Render()
        {
            //base.Render();
        }

        public static Control Add<T>(string label, object instance, PropertyInfo propertyInfo)
        {
            PropertyDrawer<T> prop;
            var attribs = propertyInfo.GetCustomAttributes<CustomPropertyDrawerAttribute>(true);//customattributes when supporting priority/overriding
                                                                                                // if (attrib is null)
            {
                if (mappedPropertyDrawers.ContainsKey(typeof(T)))
                    prop = Activator.CreateInstance(mappedPropertyDrawers[typeof(T)].type, label) as PropertyDrawer<T>;
                else if (propertyInfo.PropertyType.GetInterfaces()
    .Any(i => i == typeof(IList)))//isassignablefrom?
                {
                    prop = new ArrayDrawer(label) as PropertyDrawer<T>;
                }
                else
                    prop = Activator.CreateInstance(mappedPropertyDrawers[typeof(object)].type, label) as PropertyDrawer<T>;
            }
            prop.attributes = attribs.ToArray();
            prop.getter = DelegateGenerator.GenerateGetter<T>(instance, propertyInfo);
            prop.setter = DelegateGenerator.GenerateSetter<T>(instance, propertyInfo);
            prop.AutoSize = AutoSize.Horizontal;
            prop.Name = label;
            prop.Value = prop.getter();
            prop.propertyIsDirty = false;
            return prop;
        }

        public void RenderComponents(Entity entity)
        {
            var prop = ptree.GetControl("Transform") as ComponentNode;
            if (prop is null)
            {
                var node = new ComponentNode();
                node.Label.Text = "Transform";
                node.Label.TextAlign = Alignment.MiddleLeft;
                var props = entity.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);
                ptree.Nodes.Add(node);
                Control propDrawer;
                foreach (var entityProp in props)
                {
                    var attrib = entityProp.GetCustomAttribute<CustomPropertyDrawerAttribute>(true);
                    var attribType = attrib?.GetType();
                    if (mappedPropertyDrawers.ContainsKey(entityProp.PropertyType))
                        propDrawer = mappedPropertyDrawers[entityProp.PropertyType].method.Invoke(prop, new object[] { entityProp.Name + ":", entity, entityProp }) as Control;
                    else
                        propDrawer = mappedPropertyDrawers[typeof(object)].method.Invoke(prop, new object[] { entityProp.Name + ":", entity, entityProp }) as Control;

                    node.Frame.Controls.Add(propDrawer);
                }
            }
            var comps = entity.GetAllComponents();
            foreach (var component in comps)
            {
                prop = ptree.GetControl(component.GetType().Name) as ComponentNode;
                if (prop is null)
                {
                    var node = new ComponentNode();
                    node.Label.Text = component.GetType().Name;
                    node.Label.TextAlign = Alignment.MiddleLeft;
                    ptree.Nodes.Add(node);
                    var inspector = new DefaultComponentDrawer();
                    inspector.properties = node;
                    inspector.getTarget = component;
                    inspector.OnInitializeGUI();
                }
            }
        }

        /*  private void UpdateInspector() {
              var prop = ptree.AddOrGet("Transform", out bool exist);
              if (exist)
              {
                  prop.
              }
              var comps = entity.GetAllComponents();
              foreach (var component in comps)
              {
                  prop = ptree.AddOrGet(component.GetType().Name, out exist);
                  if (!exist)
                  {
                      Console.WriteLine("component");
                      var inspector = new DefaultComponentDrawer();
                      inspector.properties = prop;
                      inspector.getTarget = component;
                      inspector.OnInitializeGUI();
                  }
              }
          }*/

        public override void OnResize(int width, int height)
        {
            //ptree.SetBounds(0, 25, panel.s, height);
        }
    }
}