using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class ResolveByPositionAndTypeAttribute : Attribute
    {
        public ResolveByPositionAndTypeAttribute()
        {
        }
    }
}
