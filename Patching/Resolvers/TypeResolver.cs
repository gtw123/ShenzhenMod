using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;
using static System.FormattableString;

namespace ShenzhenMod.Patching.Resolvers
{
    public class TypeResolver : MemberResolver
    {
        private struct TypeAttributeInfo<TAttr> where TAttr : Attribute
        {
            public Type ReflectionType;
            public TypeReference CecilType;
            public TAttr Attribute;
        }

        public TypeResolver(Assembly sourceAssembly, MemberMap memberMap)
            : base(sourceAssembly, memberMap)
        {
        }

        public override void Resolve()
        {
            ResolveByName();
            ResolveByMethod();
        }

        private void ResolveByName()
        {
            foreach (var info in FindTypesWithAttribute<ResolveByNameAttribute>())
            {
                string targetTypeName = info.Attribute.Name ?? info.ReflectionType.Name;
                var targetType = TargetModule.GetType(targetTypeName) ?? throw new Exception(Invariant($"Cannot locate type named \"{targetTypeName}\" in target module when processing ResolveByNameAttribute on {info.ReflectionType.Name}"));

                MemberMap.AddType(info.CecilType, targetType);
            }
        }

        private void ResolveByMethod()
        {
            foreach (var info in FindTypesWithAttribute<ResolveByMethodAttribute>())
            {
                var targetType = InvokeLocatorMethod<TypeDefinition>(info.Attribute, info.ReflectionType, TargetModule);
                MemberMap.AddType(info.CecilType, targetType);
            }
        }

        private IEnumerable<TypeAttributeInfo<TAttr>> FindTypesWithAttribute<TAttr>() where TAttr : Attribute
        {
            return from type in SourceAssembly.GetTypes()
                   from attribute in type.GetCustomAttributes<TAttr>()
                   select new TypeAttributeInfo<TAttr> { ReflectionType = type, CecilType = SourceModule.ImportReference(type), Attribute = attribute };
        }
    }
}
