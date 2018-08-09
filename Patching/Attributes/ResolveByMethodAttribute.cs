using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct)]
    public class ResolveByMethodAttribute : Attribute
    {
        public string Method { get; private set; }
        public string Class { get; set; }

        public ResolveByMethodAttribute(string method = null)
        {
            Method = method;
        }
    }
}
