using System;

namespace Sharp.Core.Editor.Attribs
{
    class PriorityAttribute : Attribute
    {
        public readonly short priority;
        public PriorityAttribute(short pri)
        {

        }
    }
}
