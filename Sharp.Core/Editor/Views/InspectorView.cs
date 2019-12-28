using Sharp.Editor.Attribs;
using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Sharp.Editor.Views
{
	public class InspectorView : View
	{
		//private TreeView ptree = new TreeView();
		internal Guid currentlyDrawedObject = default;

		//private ListBox tagStrip = new ListBox();
		private DropDownButton tagStrip = new DropDownButton();
		private Dictionary<Guid, TreeView> idToViewMapping = new Dictionary<Guid, TreeView>();
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
			/*tagStrip.Size = new Point(0, 25);
			tagStrip.Dock = DockStyle.FillX;
			tagStrip.Text = "Tags";
			tagStrip.Style = "label";
			tagStrip.Dropdown.Style = "";
			tagStrip.Parent = this;

			var button = new Button();
			button.Text = "Add tag";
			button.Parent = tagStrip.Dropdown;
			button.Dock = DockStyle.FillX;
			button.TextAlign = Alignment.MiddleLeft;*/ //make it as component
			if (idToViewMapping.Count is 0)
				foreach (var entity in Extension.entities.root)
				{
					RegisterEngineObject(entity);
					var comps = entity.GetAllComponents();
					if (comps is { })
						foreach (var component in comps)
						{
							RegisterEngineObject(component);
						}
				}


			Selection.OnSelectionChange += (old, @new) =>
			{
				Console.WriteLine("SelectionChange" + @new);
				if (@new is IEngineObject obj)
				{
					if (idToViewMapping.TryGetValue(currentlyDrawedObject, out var oldSelected))
					{
						oldSelected.IsVisible = false;
					}
					currentlyDrawedObject = @new.GetInstanceID();
					idToViewMapping[currentlyDrawedObject].IsVisible = true;
				}
				else if (@new is null)
				{
					if (idToViewMapping.TryGetValue(currentlyDrawedObject, out var oldSelected))
					{
						oldSelected.IsVisible = false;
					}
					currentlyDrawedObject = default;
				}
				Squid.UI.isDirty = true;
			};

			/*Selection.OnSelectionDirty += (sender) =>
			{
				//if (sender is Entity entity) RenderComponents(entity);
			};*/
			Button.Text = "Inspector";
			SceneView.onAddedEntity += ItemAdded;
			SceneView.onRemovedEntity += ItemRemoved;
		}
		private void ItemAdded(IEngineObject sender)
		{
			RegisterEngineObject(sender);
		}
		private void ItemRemoved(IEngineObject sender)
		{
			if (sender is Entity ent)
			{
				idToViewMapping[ent.GetInstanceID()].Parent = null;
				idToViewMapping.Remove(ent.GetInstanceID());
			}
			else if (sender is Component component)
				idToViewMapping[component.Parent.GetInstanceID()].Nodes.Remove(idToViewMapping[component.Parent.GetInstanceID()].Nodes.Find((node) => ((node.Childs[2] as FlowLayoutFrame).Controls[0] as PropertyDrawer).Target == component));

		}
		private void RegisterEngineObject(IEngineObject obj)
		{

			if (obj is Entity ent)
			{
				var ptree = new TreeView();
				ptree.Dock = DockStyle.Fill;
				ptree.Margin = new Margin(0, 25, 0, 0);
				ptree.Parent = this;
				ptree.Scrollbar.Size = new Point(0, 0);
				ptree.Scissor = false;
				ptree.IsVisible = false;
				idToViewMapping.Add(ent.GetInstanceID(), ptree);
			}
			else if (obj is Component component)
				idToViewMapping[component.Parent.GetInstanceID()].Nodes.Add(RenderComponent(component));
			//else if(obj is System sys)//TODO: rewrite EC to ECS
		}
		private ComponentNode RenderComponent(Component component)
		{
			var node = new ComponentNode();
			node.Label.Text = component.GetType().Name;
			node.Name = component.GetType().Name;
			node.Label.TextAlign = Alignment.MiddleLeft;

			var inspector = new DefaultComponentDrawer();
			inspector.properties = node;
			inspector.getTarget = component;
			inspector.OnInitializeGUI();
			return node;
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

		public static PropertyDrawer Add(MemberInfo propertyInfo)
		{
			PropertyDrawer prop = null;
			var attribs = propertyInfo.GetCustomAttributes<CustomPropertyDrawerAttribute>(true);//customattributes when supporting priority/overriding
																								// if (attrib is null)
			{
				if (mappedPropertyDrawers.ContainsKey(propertyInfo.GetUnderlyingType()))
					prop = Activator.CreateInstance(mappedPropertyDrawers[propertyInfo.GetUnderlyingType()], propertyInfo) as PropertyDrawer;
				else if (propertyInfo.GetUnderlyingType().GetInterfaces()
	.Any(i => i == typeof(IList)))//isassignablefrom?
				{
					prop = new ArrayDrawer(propertyInfo);
				}
				else prop = Activator.CreateInstance(typeof(InvisibleSentinel<>).MakeGenericType(propertyInfo.GetUnderlyingType().IsByRef ? propertyInfo.GetUnderlyingType().GetElementType() : propertyInfo.GetUnderlyingType()), propertyInfo) as PropertyDrawer;
			}
			//prop.attributes = attribs.ToArray();
			prop.AutoSize = AutoSize.Horizontal;
			return prop;
		}

		/*public void RenderComponents(Entity entity)
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
		}*/

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