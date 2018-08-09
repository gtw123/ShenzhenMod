using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class ResolveBySignatureAttribute : Attribute
    {
        public ResolveBySignatureAttribute()
        {
        }
    }
}
