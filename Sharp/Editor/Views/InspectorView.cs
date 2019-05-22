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
		private Guid currentlyDrawedObject;

		//private ListBox tagStrip = new ListBox();
		private DropDownButton tagStrip = new DropDownButton();
		private Dictionary<Guid, TreeView> idToViewMapping = new Dictionary<Guid, TreeView>();
		internal static Dictionary<Type, Type> mappedPropertyDrawers = new Dictionary<Type, Type>();
		internal static Dictionary<(Guid, string), string> availableUndoRedo;
		internal static Dictionary<(Guid, string), string> saveState;
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

			Coroutine.Start(SaveChangesBeforeNextFrame());
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
			{
				foreach (var entity in Extension.entities.root)
					RegisterEngineObject(entity);
			}

			Selection.OnSelectionChange += (sender) =>
			{
				Console.WriteLine("SelectionChange" + sender);
				if (sender is IEngineObject obj)
				{
					if (idToViewMapping.TryGetValue(currentlyDrawedObject, out var oldSelected))
					{
						oldSelected.IsVisible = false;
					}
					currentlyDrawedObject = sender.GetInstanceID();
					idToViewMapping[currentlyDrawedObject].IsVisible = true;
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

		private static IEnumerator SaveChangesBeforeNextFrame()
		{
			while (true)
			{
				yield return new WaitForEndOfFrame();
				if (saveState is { })
				{
					SaveChanges(saveState);
					saveState = null;
				}
				if (availableUndoRedo is { })
				{
					if (availableUndoRedo.ContainsValue("selection"))
						Selection.Asset = Extension.entities.idToObjectMapping[availableUndoRedo.GetKey("selection").Item1];

					availableUndoRedo = null;
				}
			}
		}
		private static void SaveChanges(Dictionary<(Guid, string), string> toBeSaved)
		{
			if (UndoCommand.currentHistory is { })
			{
				if (UndoCommand.currentHistory != UndoCommand.snapshots.Last)
				{
					UndoCommand.currentHistory.RemoveAllAfter();
					Console.WriteLine("clear trailing history");
				}
			}
			var toBeSeparated = new Dictionary<(Guid, string), string>();
			foreach (var (index, val) in toBeSaved)
				if (index.Item2 == "addedEntity" || index.Item2 == "addedComponent" || index.Item2 == "addedSystem" || index.Item2 == "Parent")
					toBeSeparated.Add(index, val);
			Console.WriteLine("separate " + toBeSeparated.Any());
			if (toBeSeparated.Any())
			{
				UndoCommand.snapshots.AddLast(new History() { propertyMapping = toBeSeparated, onlyAdditionOrSubtraction = true });
				foreach (var (remove, _) in toBeSeparated)
					toBeSaved.Remove(remove);
			}
			UndoCommand.snapshots.AddLast(new History() { propertyMapping = toBeSaved });
			UndoCommand.currentHistory = UndoCommand.snapshots.Last;
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
			{
				idToViewMapping[component.Parent.GetInstanceID()].Nodes.Remove(idToViewMapping[component.Parent.GetInstanceID()].Nodes.Find((node) => (node as ComponentNode).referencedComponent == component));
			}
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
				var comps = ent.GetAllComponents();
				if (comps is { })
					foreach (var component in comps)
						ptree.Nodes.Add(RenderComponent(component));

				ptree.IsVisible = false;
				idToViewMapping.Add(ent.GetInstanceID(), ptree);
				if (availableUndoRedo is null || !availableUndoRedo.ContainsKey((ent.GetInstanceID(), "addedEntity")))
				{
					if (saveState is null)
						saveState = new Dictionary<(Guid, string), string>();
					saveState.Add((ent.GetInstanceID(), "addedEntity"), ent.name);//TODO: add IEnumerable for mass placing
				}
			}
			else if (obj is Component component)
			{
				idToViewMapping[component.Parent.GetInstanceID()].Nodes.Add(RenderComponent(component));
				if (availableUndoRedo is null || !availableUndoRedo.ContainsKey((component.GetInstanceID(), "addedComponent")))
				{
					if (saveState is null)
						saveState = new Dictionary<(Guid, string), string>();
					saveState.Add((component.GetInstanceID(), "addedComponent"), component.GetType().AssemblyQualifiedName);
					saveState.Add((component.GetInstanceID(), "Parent"), component.Parent.GetInstanceID().ToString());
				}
			}
			//else if(obj is System sys)//TODO: rewrite EC to ECS
		}
		private ComponentNode RenderComponent(Component component)
		{
			var node = new ComponentNode();
			node.Label.Text = component.GetType().Name;
			node.Name = component.GetType().Name;
			node.Label.TextAlign = Alignment.MiddleLeft;
			node.referencedComponent = component;
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

		public static PropertyDrawer Add(string label, object instance, MemberInfo propertyInfo)
		{
			PropertyDrawer prop = null;
			var attribs = propertyInfo.GetCustomAttributes<CustomPropertyDrawerAttribute>(true);//customattributes when supporting priority/overriding
																								// if (attrib is null)
			{
				if (mappedPropertyDrawers.ContainsKey(propertyInfo.GetUnderlyingType()))
					prop = Activator.CreateInstance(mappedPropertyDrawers[propertyInfo.GetUnderlyingType()], label,propertyInfo) as PropertyDrawer;
				else if (propertyInfo.GetUnderlyingType().GetInterfaces()
	.Any(i => i == typeof(IList)))//isassignablefrom?
				{
					prop = new ArrayDrawer(label,propertyInfo);
				}
				else prop= new InvisibleSentinel(label,propertyInfo);
				//    prop = Activator.CreateInstance(mappedPropertyDrawers[typeof(object)].type, label) as PropertyDrawer;
			}
			prop.attributes = attribs.ToArray();
			prop.memberInfo = propertyInfo;
			prop.AutoSize = AutoSize.Horizontal;
			prop.Name = label;
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