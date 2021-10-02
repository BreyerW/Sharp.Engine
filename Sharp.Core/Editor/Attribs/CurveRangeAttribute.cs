using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharp.Editor.Attribs
{
    public class CurveRangeAttribute : Attribute
    {
        public (float x, float y, float width, float height) curvesRange;

        public CurveRangeAttribute(float x, float y, float width, float height)
        {
            curvesRange = (x, y, width, height);
        }
    }
}