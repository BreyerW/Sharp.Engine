using System;
using System.Collections.Generic;

namespace SharpAsset.StyleProperties
{
    [CSSEquivalent("padding")]
    public struct Padding : IStyleProperty
    {
        [CSSEquivalent("padding-top")]
        public float top;

        public void ApplyProperty()
        {
            throw new NotImplementedException();
        }
    }
}