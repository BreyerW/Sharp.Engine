namespace Sharp.Editor.Attribs
{
    public class RangeAttribute : CustomPropertyDrawerAttribute
    {
        public float min;
        public float max;

        public RangeAttribute(float min, float max)
        {
        }
    }
}