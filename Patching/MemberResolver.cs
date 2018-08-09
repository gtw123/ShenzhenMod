using System;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;
using static System.FormattableString;

namespace ShenzhenMod.Patching
{
    public abstract class MemberResolver
    {
        public Assembly SourceAssembly { get; private set; }
        public MemberMap MemberMap { get; private set; }

        public ModuleDefinition SourceModule => MemberMap.SourceModule;
        public ModuleDefinition TargetModule => MemberMap.TargetModule;

        public MemberResolver(Assembly sourceAssembly, MemberMap memberMap)
        {
            SourceAssembly = sourceAssembly;
            MemberMap = memberMap;
        }

        public abstract void Resolve();

        protected TResult InvokeLocatorMethod<TResult>(ResolveByMethodAttribute attr, Type defaultClass, object param)
        {
            var locatorClass = (attr.Class != null) ? SourceAssembly.GetType(attr.Class, throwOnError: true) : defaultClass;
            var locatorMethod = locatorClass.GetMethod(attr.Method, BindingFlags.Public | BindingFlags.Static)
                ?? throw new Exception(Invariant($"Cannot locate method \"{attr.Method}\" on type \"{locatorClass}\""));

            return (TResult)locatorMethod.Invoke(null, new object[] { param });
        }
    }
}
