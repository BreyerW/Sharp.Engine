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

namespace Sharp.Editor.Views
{
	public class InspectorView : View
	{
		//private TreeView ptree = new TreeView();
		internal object currentlyDrawedObject;

		//private ListBox tagStrip = new ListBox();
		private DropDownButton tagStrip = new DropDownButton();
		private Dictionary<Entity, TreeView> idToViewMapping = new Dictionary<Entity, TreeView>();
		internal static Dictionary<Type, SortedSet<Type>> mappedPropertyDrawers = new Dictionary<Type, SortedSet<Type>>();
		internal static Dictionary<Type, SortedSet<Type>> mappedComponentDrawers = new Dictionary<Type, SortedSet<Type>>();

		static InspectorView()
		{
			var result = Assembly.GetExecutingAssembly().GetTypes()
			 .Where(t => t.IsSubclassOfOpenGeneric(typeof(PropertyDrawer<>)));

			Type[] genericArgs;
			var priComparer = new PriorityComparer();
			foreach (var type in result)
			{
				genericArgs = type.BaseType.GetGenericArguments();
				if (!mappedPropertyDrawers.TryGetValue(genericArgs[0], out var set))
					mappedPropertyDrawers.Add(genericArgs[0], new SortedSet<Type>(priComparer) { type });
				else
					set.Add(type);
			}
			result = Assembly.GetExecutingAssembly().GetTypes()
			 .Where(t => t.IsSubclassOfOpenGeneric(typeof(ComponentDrawer<>)));


			foreach (var type in result)
			{
				genericArgs = type.BaseType.GetGenericArguments();
				if (!mappedComponentDrawers.TryGetValue(genericArgs[0], out var set))
					mappedComponentDrawers.Add(genericArgs[0], new SortedSet<Type>(priComparer) { type });
				else
					set.Add(type);
			}
		}
		private class PriorityComparer : IComparer<Type>
		{
			public int Compare(Type x, Type y)
			{
				var xPri = x.GetCustomAttribute<PriorityAttribute>();
				var yPri = y.GetCustomAttribute<PriorityAttribute>();
				if (xPri is null && yPri is null) return 0;
				if (xPri is null && yPri is not null) return -1;
				if (xPri is not null && yPri is null) return 1;
				return xPri.priority < yPri.priority ? 1 : -1;
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


			Selection.OnSelectionChange += (old, @new) =>
			{
				Console.WriteLine("SelectionChange" + @new);
				if (@new is Entity obj)
				{
					if (currentlyDrawedObject is not null && idToViewMapping.TryGetValue(currentlyDrawedObject as Entity, out var oldSelected))
					{
						oldSelected.IsVisible = false;
					}
					currentlyDrawedObject = @new;
					idToViewMapping[currentlyDrawedObject as Entity].IsVisible = true;
				}
				else if (@new is null)
				{
					if (idToViewMapping.TryGetValue(currentlyDrawedObject as Entity, out var oldSelected))
					{
						oldSelected.IsVisible = false;
					}
					currentlyDrawedObject = default;
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
		private void RegisterEngineObject(IEngineObject obj)
		{
			if (obj is Entity ent)
			{
				//if (idToViewMapping.ContainsKey(ent.GetInstanceID())) return;
				var ptree = new TreeView();
				ptree.Dock = DockStyle.Fill;
				ptree.Margin = new Margin(0, 25, 0, 0);
				ptree.Parent = this;
				ptree.Scrollbar.Size = new Point(0, 0);
				ptree.Scissor = false;
				ptree.IsVisible = false;
				idToViewMapping.TryAdd(ent, ptree);
			}
			else if (obj is Component component)
			{
				idToViewMapping[component.Parent].Nodes.Add(RenderComponent(component));
			}
			//else if(obj is System sys)//TODO: rewrite EC to ECS
		}
		private ComponentNode RenderComponent(Component component)
		{
			ComponentDrawer draw = null;                                                                 // if (attrib is null)

			if (mappedComponentDrawers.TryGetValue(component.GetType(), out var set) || mappedComponentDrawers.TryGetValue(typeof(Component), out set))
			{
				foreach (var drawer in set)
				{
					draw = Activator.CreateInstance(drawer) as ComponentDrawer;
					if (draw.CanApply(component.GetType()))
					{
						draw.Target = component;
						draw.OnInitializeGUI();
						break;
					}
				}
			}
			draw.Label.Text = component.GetType().Name;
			draw.Name = component.GetType().Name;
			draw.Label.TextAlign = Alignment.MiddleLeft;
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

		public static PropertyDrawer Add(MemberInfo propertyInfo)
		{
			PropertyDrawer prop = null;                                                                 // if (attrib is null)

			if (mappedPropertyDrawers.TryGetValue(propertyInfo.GetUnderlyingType(), out var set))
			{
				foreach (var drawer in set)
				{
					prop = Activator.CreateInstance(drawer, propertyInfo) as PropertyDrawer;
					if (prop.CanApply(propertyInfo))
						break;
				}
			}
			else if (propertyInfo.GetUnderlyingType().GetInterfaces()
.Any(i => i == typeof(IList)))//isassignablefrom?
			{
				prop = new ArrayDrawer(propertyInfo);
			}
			else return null; //prop = Activator.CreateInstance(typeof(InvisibleSentinel<>).MakeGenericType(propertyInfo.GetUnderlyingType().IsByRef ? propertyInfo.GetUnderlyingType().GetElementType() : propertyInfo.GetUnderlyingType()), propertyInfo) as PropertyDrawer;

			//prop.attributes = attribs.ToArray();
			prop.AutoSize = AutoSize.Horizontal;
			return prop;
		}


	}
}