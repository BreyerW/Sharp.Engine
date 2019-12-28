using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Editor.Attribs
{
    [AttributeUsage(AttributeTargets.Property)]
    public abstract class CustomPropertyDrawerAttribute : Attribute
    {
        //public abstract Type BindToPropertyDrawer();
    }
}