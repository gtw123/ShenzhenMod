using System;

namespace ShenzhenMod.Patching.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Method | AttributeTargets.Struct)]
    public class ResolveByNameAttribute : Attribute
    {
        public string Name { get; private set; }

        public ResolveByNameAttribute(string name = null)
        {
            Name = name;
        }
    }
}
