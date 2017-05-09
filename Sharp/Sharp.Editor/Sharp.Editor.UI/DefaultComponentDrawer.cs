using System;
using System.Collections.Generic;
using Sharp.Control;
using Sharp.Editor.Views;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Sharp.Editor;

namespace Sharp.Editor.UI
{
    //[CustomInspector(typeof(object))]
    public class DefaultComponentDrawer : ComponentDrawer<object>
    {
        public override void OnInitializeGUI()//OnSelect
        {
            var props = Target.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite);
            //TypedReference.MakeTypedReference();

            foreach (var prop in props)
            {
                BindProperty(prop);
            }
        }
    }
}