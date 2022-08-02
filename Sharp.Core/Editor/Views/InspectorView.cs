using Sharp.Core.Editor.Attribs;
using Sharp.Editor.Attribs;
using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using Squid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Sharp.Editor.Views
{
	public class InspectorView : View
	{
		//private TreeView ptree = new TreeView();
		internal List<object> currentlyDrawedObjects = new();

		//private ListBox tagStrip = new ListBox();
		private DropDownButton tagStrip = new DropDownButton();
		private Dictionary<Entity, TreeView> idToViewMapping = new Dictionary<Entity, TreeView>();
		internal static Dictionary<Type, Func<PropertyDrawer>> mappedPropertyDrawers = new();
		internal static Dictionary<Type, Func<ComponentDrawer>> mappedComponentDrawers = new();
		public static void RegisterDrawerFor<T>(Func<PropertyDrawer> factory)
		{
			//if (mappedPropertyDrawers.TryGetValue(typeof(T), out var f) is false)
			//	f = factory;
			ref var loc = ref CollectionsMarshal.GetValueRefOrAddDefault(mappedPropertyDrawers, typeof(T), out var exist);
			if (exist is false)
				loc = factory;
		}
		public static void RegisterDrawerFor<T>(Func<ComponentDrawer> factory) where T : Component
		{
			//if (mappedComponentDrawers.TryGetValue(typeof(T), out var f) is false)
			//	f = factory;
			ref var loc = ref CollectionsMarshal.GetValueRefOrAddDefault(mappedComponentDrawers, typeof(T), out var exist);
			if (exist is false)
				loc = factory;
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


			ListWrapper.OnListChange += (n) =>
			 {
				 foreach (var o in currentlyDrawedObjects)
					 if (idToViewMapping.TryGetValue(o as Entity, out var oldSelected))
					 {
						 oldSelected.IsVisible = false;
					 }
				 currentlyDrawedObjects.Clear();
				 currentlyDrawedObjects.InsertRange(0, n);
				 if (n is null or { Count: 0 })
					 return;

				 foreach (var o in n)
				 {
					 if (o is Entity obj)
					 {
						 Console.WriteLine("SelectionChange" + o);
						 idToViewMapping[obj].IsVisible = true;
					 }
				 }
				 Squid.UI.isDirty = true;
			 };
			AllowFocus = true;
			/*Selection.OnSelectionDirty += (sender) =>
			{
				//if (sender is Entity entity) RenderComponents(entity);
			};*/
			Button.Text = "Inspector";
		}
		protected override void OnLateUpdate()
		{
			base.OnLateUpdate();
			foreach (var added in Root.addedEntities)
				RegisterEngineObject(added);

			foreach (var removal in Root.removedEntities)
				if (removal is Entity ent)
				{
					idToViewMapping.Remove(ent);
				}
				else if (removal is Component component)
					if (idToViewMapping.TryGetValue(component.Parent, out var tree))
						tree.Nodes.Remove(idToViewMapping[component.Parent].Nodes.Find((node) => node.UserData == component));

		}
		//obj is Component { Parent : var ent} or Entity ent
		private void RegisterEngineObject(IEngineObject obj)
		{
			if (obj is Entity ent)
			{
				if (idToViewMapping.TryGetValue(ent, out _) is false)
					CreateNewTreeView(ent);
			}
			else if (obj is Component component)
			{
				if (idToViewMapping.TryGetValue(component.Parent, out _) is false)
				{
					CreateNewTreeView(component.Parent);
				}
				idToViewMapping[component.Parent].Nodes.Add(RenderComponent(component));
			}
			//else if(obj is System sys)//TODO: rewrite EC to ECS
		}
		private void CreateNewTreeView(Entity e)
		{
			var ptree = new TreeView();
			ptree.Dock = DockStyle.Fill;
			ptree.Margin = new Margin(0, 25, 0, 0);
			ptree.Parent = this;
			ptree.Scrollbar.Size = new Point(0, 0);
			ptree.Scissor = false;
			ptree.IsVisible = false;
			idToViewMapping.TryAdd(e, ptree);
		}
		private ComponentNode RenderComponent(Component component)
		{
			ComponentDrawer draw = null;                                                                 // if (attrib is null)

			if (mappedComponentDrawers.TryGetValue(component.GetType(), out var factory) || mappedComponentDrawers.TryGetValue(typeof(Component), out factory))
			{
				draw = factory();
				draw.Target = component;
				draw.OnInitializeGUI();

				draw.Label.Text = component.GetType().Name;
				draw.Name = component.GetType().Name;
				draw.Label.TextAlign = Alignment.MiddleLeft;
			}
			return draw;
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

		/*public static PropertyDrawer Add(MemberInfo propertyInfo)
		{
			//TODO: add support for attributes
			PropertyDrawer prop = null;
			if (mappedPropertyDrawers.TryGetValue(propertyInfo.GetUnderlyingType(), out var factory))
			{
				prop = factory();
			}
			else if (propertyInfo.GetUnderlyingType().IsAssignableTo(typeof(IList))/*.GetInterfaces()
.Any(i => i == typeof(IList))*)//isassignablefrom?
			{
				prop = new ArrayDrawer();
			}
			else return null; //prop = Activator.CreateInstance(typeof(InvisibleSentinel<>).MakeGenericType(propertyInfo.GetUnderlyingType().IsByRef ? propertyInfo.GetUnderlyingType().GetElementType() : propertyInfo.GetUnderlyingType()), propertyInfo) as PropertyDrawer;

			//prop.attributes = attribs.ToArray();
			prop.AutoSize = AutoSize.Horizontal;
			return prop;
		}*/


	}
}