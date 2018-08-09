using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ResolveByTypeAttribute : Attribute
    {
        public ResolveByTypeAttribute()
        {
        }
    }
}
