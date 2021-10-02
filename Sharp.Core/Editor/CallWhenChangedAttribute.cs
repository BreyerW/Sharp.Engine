using System;

namespace Sharp.Core.Editor
{
    class CallWhenChangedAttribute : Attribute
    {
        public string[] methodsToCall;
        public CallWhenChangedAttribute(params string[] methods)
        {
            methodsToCall = methods;
        }
    }
}
