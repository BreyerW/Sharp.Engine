using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Linq.Expressions;
using Gwen.Control;
using Sharp.Editor.UI;
using Sharp.Editor.UI.Property;
using Sharp.Control;

namespace Sharp.Editor.Views
{
    public class InspectorView : View
    {
        private PropertyTree ptree;
        private MenuStrip tagStrip;

        public InspectorView(uint attachToWindow) : base(attachToWindow)
        {
        }

        public override void Initialize()
        {
            tagStrip = new MenuStrip(panel);
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
            Selection.OnSelectionChange += (sender, args) =>
            {
                Console.WriteLine("SelectionChange");
                ptree.RemoveAll();
                if (sender is Entity entity) RenderComponents(entity);
                //else
                //props=Selection.assets [0].GetType ().GetProperties ().Where (p=>p.CanRead && p.CanWrite);

                ptree.Show();
                ptree.SetBounds(0, 0, panel.Width, panel.Height);
                ptree.ExpandAll();
                tagStrip.Show();
            };
            Selection.OnSelectionDirty += (sender, args) =>
            {
                if (sender is Entity entity) RenderComponents(entity);
            };
            base.Initialize();
        }

        public void RenderComponents(Entity entity)
        {
            var prop = ptree.AddOrGet("Transform", out bool exist);
            if (!exist)
            {
                var props = entity.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);

                foreach (var entityProp in props)
                {
                    var attrib = entityProp.GetCustomAttribute<CustomPropertyDrawerAttribute>(true);
                    var attribType = attrib?.GetType();
                    if (Properties.mappedPropertyDrawers.ContainsKey((entityProp.PropertyType, attribType)))
                        Properties.mappedPropertyDrawers[(entityProp.PropertyType, attribType)].method.Invoke(prop, new object[] { entityProp.Name + ":", entity, entityProp });
                    else
                        Properties.mappedPropertyDrawers[(typeof(object), attribType)].method.Invoke(prop, new object[] { entityProp.Name + ":", entity, entityProp });
                }
            }
            var comps = entity.GetAllComponents();
            foreach (var component in comps)
            {
                prop = ptree.AddOrGet(component.GetType().Name, out exist);
                if (!exist)
                {
                    var inspector = new DefaultComponentDrawer();
                    inspector.properties = prop;
                    inspector.getTarget = component;
                    inspector.OnInitializeGUI();
                }
            }
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
            ptree.SetBounds(0, 25, panel.Width, height);
        }
    }
}