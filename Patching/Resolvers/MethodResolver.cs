using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;
using static System.FormattableString;

namespace ShenzhenMod.Patching.Resolvers
{
    public class MethodResolver : MemberResolver
    {
        private struct MethodAttributeInfo<TAttr> where TAttr : Attribute
        {
            public MethodInfo ReflectionMethod;
            public MethodReference CecilMethod;
            public TAttr Attribute;
        }

        public MethodResolver(Assembly sourceAssembly, MemberMap memberMap)
            : base(sourceAssembly, memberMap)
        {
        }

        public override void Resolve()
        {
            ResolveByName();
            ResolveByMethod();
            ResolveBySignature();
        }

        private void ResolveByName()
        {
            foreach (var info in FindMethodsWithAttribute<ResolveByNameAttribute>())
            {
                var targetType = MemberMap.GetTargetType(info.CecilMethod.DeclaringType);

                string targetMethodName = info.Attribute.Name ?? info.ReflectionMethod.Name;
                var targetMethod = targetType.FindMethod(targetMethodName) ?? throw new Exception(Invariant($"Cannot locate method named \"{targetMethodName}\" in type \"{targetType.Name}\" in target module"));

                MemberMap.AddMethod(info.CecilMethod, targetMethod);
            }
        }

        private void ResolveByMethod()
        {
            foreach (var info in FindMethodsWithAttribute<ResolveByMethodAttribute>())
            {
                var targetType = MemberMap.GetTargetType(info.CecilMethod.DeclaringType);
                var targetMethod = InvokeLocatorMethod<MethodDefinition>(info.Attribute, info.ReflectionMethod.DeclaringType, targetType);

                MemberMap.AddMethod(info.CecilMethod, targetMethod);
            }
        }

        private void ResolveBySignature()
        {
            foreach (var info in FindMethodsWithAttribute<ResolveBySignatureAttribute>())
            {
                var method = info.ReflectionMethod;
                var sourceMethod = info.CecilMethod;
                var targetType = MemberMap.GetTargetType(info.CecilMethod.DeclaringType);

                // TODO: Make it work with generic instance methods
                var returnType = MemberMap.ConvertType(sourceMethod.ReturnType).FullName;
                var parameters = sourceMethod.Parameters.Select(p => MemberMap.ConvertType(p.ParameterType).FullName);
                var targetMethod = targetType.Methods.Single(m => m.IsAbstract == method.IsAbstract && m.IsStatic == method.IsStatic && m.IsPublic == method.IsPrivate == m.IsPrivate
                    && m.ReturnType.FullName == returnType && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(parameters));
                MemberMap.AddMethod(sourceMethod, targetMethod);
            }
        }

        private IEnumerable<MethodAttributeInfo<TAttr>> FindMethodsWithAttribute<TAttr>() where TAttr : Attribute
        {
            return from type in SourceAssembly.GetTypes()
                   from method in type.GetRuntimeMethods()
                   from attribute in method.GetCustomAttributes<TAttr>()
                   select new MethodAttributeInfo<TAttr> { ReflectionMethod = method, CecilMethod = SourceModule.ImportReference(method).RemoveGenericArguments(), Attribute = attribute };
        }
    }
}
