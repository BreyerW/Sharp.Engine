using System.Linq;

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