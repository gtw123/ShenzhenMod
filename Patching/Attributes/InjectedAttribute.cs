using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct)]
    public class InjectedAttribute : Attribute
    {
        public InjectedAttribute()
        {
        }
    }
}
