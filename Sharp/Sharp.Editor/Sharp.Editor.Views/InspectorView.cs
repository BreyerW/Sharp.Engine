using Sharp.Editor.Attribs;
using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Sharp.Editor.Views
{
	public class InspectorView : View
	{
		private TreeView ptree = new TreeView();
		private Guid currentlyDrawedObject;

		//private ListBox tagStrip = new ListBox();
		private DropDownButton tagStrip = new DropDownButton();

		internal static Dictionary<Type, Type> mappedPropertyDrawers = new Dictionary<Type, Type>();

		static InspectorView()
		{
			//var primitiveResult = Assembly.GetExecutingAssembly()
			//   .GetTypes()
			// .Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
			//      t.BaseType.GetGenericTypeDefinition() == typeof(Gwen.Control.Property.PropertyDrawer<>));//current solution does not support color case

			var result = Assembly.GetExecutingAssembly().GetTypes()
			 .Where(t => t.IsSubclassOfOpenGeneric(typeof(PropertyDrawer<>)));

			Type[] genericArgs;

			foreach (var type in result)
			{
				genericArgs = type.BaseType.GetGenericArguments();
				mappedPropertyDrawers.Add(genericArgs[0], type);
			}
		}

		public InspectorView(uint attachToWindow) : base(attachToWindow)
		{
			tagStrip.Size = new Point(0, 25);
			tagStrip.Dock = DockStyle.FillX;
			tagStrip.Text = "Tags";
			tagStrip.Style = "label";
			tagStrip.Dropdown.Style = "";
			tagStrip.Parent = this;

			var button = new Button();
			button.Text = "Add tag";
			button.Parent = tagStrip.Dropdown;
			button.Dock = DockStyle.FillX;
			button.TextAlign = Alignment.MiddleLeft;

			ptree.Dock = DockStyle.Fill;
			ptree.Margin = new Margin(0, 25, 0, 0);
			ptree.Parent = this;
			ptree.Scrollbar.Size = new Point(0, 0);
			ptree.Scissor = false;
			Selection.OnSelectionChange += (sender) =>
			{
				Console.WriteLine("SelectionChange");
				if (sender is IEngineObject obj)
				{
					currentlyDrawedObject = sender.GetInstanceID();
				}
				//ptree.Nodes.Clear();
				if (sender is Entity entity) RenderComponents(entity);
				//else
				//props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);

				//ptree.ExpandAll();
				Squid.UI.isDirty = true;
			};
			Selection.OnSelectionDirty += (sender) =>
			{
				//if (sender is Entity entity) RenderComponents(entity);
			};
			Button.Text = "Inspector";
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

		public static PropertyDrawer Add(string label, object instance, PropertyInfo propertyInfo)
		{
			PropertyDrawer prop = null;
			var attribs = propertyInfo.GetCustomAttributes<CustomPropertyDrawerAttribute>(true);//customattributes when supporting priority/overriding
																								// if (attrib is null)
			{
				if (mappedPropertyDrawers.ContainsKey(propertyInfo.GetUnderlyingType()))
					prop = Activator.CreateInstance(mappedPropertyDrawers[propertyInfo.GetUnderlyingType()], label) as PropertyDrawer;
				else if (propertyInfo.PropertyType.GetInterfaces()
	.Any(i => i == typeof(IList)))//isassignablefrom?
				{
					prop = new ArrayDrawer(label);
				}
				else return null;
				//    prop = Activator.CreateInstance(mappedPropertyDrawers[typeof(object)].type, label) as PropertyDrawer;
			}
			prop.attributes = attribs.ToArray();
			prop.memberInfo = propertyInfo;
			prop.AutoSize = AutoSize.Horizontal;
			prop.Name = label;
			return prop;
		}

		public void RenderComponents(Entity entity)
		{
			var comps = entity.GetAllComponents();

			foreach (var component in comps)
			{
				var prop = ptree.GetControl(component.GetType().Name) as ComponentNode;
				if (prop is null)
				{
					var node = new ComponentNode();
					node.Label.Text = component.GetType().Name;
					node.Name = component.GetType().Name;
					node.Label.TextAlign = Alignment.MiddleLeft;
					node.referencedComponent = component;
					ptree.Nodes.Add(node);
					var inspector = new DefaultComponentDrawer();
					inspector.properties = node;
					inspector.getTarget = component;
					inspector.OnInitializeGUI();
				}
				else
					prop.referencedComponent = component;
			}
			List<ComponentNode> toBeRemoved = new List<ComponentNode>();
			foreach (ComponentNode node in ptree.Nodes)
				if (node.Name != "Transform" && !comps.Contains(node.referencedComponent as Component)) toBeRemoved.Add(node);

			foreach (var remove in toBeRemoved)
				ptree.Nodes.Remove(remove);
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
	}
}