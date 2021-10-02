using System;

namespace Sharp.Editor.Attribs
{
    public class RangeAttribute : Attribute
    {
        public float min;
        public float max;

        public RangeAttribute(float min, float max)
        {
        }
    }
}