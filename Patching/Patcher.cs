using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patching.Resolvers;

namespace ShenzhenMod.Patching
{
    public class Patcher : IDisposable
    {
        public ModuleDefinition SourceModule { get; private set; }
        public ModuleDefinition TargetModule { get; private set; }

        public MemberMap MemberMap { get; private set; }
        private MethodPatcher m_methodPatcher;

        public Patcher(Assembly sourceAssembly, string targetAssemblyPath)
        {
            SourceModule = ModuleDefinition.ReadModule(sourceAssembly.Location);
            TargetModule = ModuleDefinition.ReadModule(targetAssemblyPath);

            MemberMap = new MemberMap(SourceModule, TargetModule);
            m_methodPatcher = new MethodPatcher(MemberMap);

            new TypeResolver(sourceAssembly, MemberMap).Resolve();
            new MethodResolver(sourceAssembly, MemberMap).Resolve();
            new FieldResolver(sourceAssembly, MemberMap).Resolve();
        }

        public void Dispose()
        {
            SourceModule.Dispose();
            TargetModule.Dispose();
        }

        public void InjectMembers()
        {
            InjectFields();
            InjectMethods();
        }

        private void InjectMethods()
        {
            foreach (var method in FindMembersWithAttribute(SourceModule.Types.SelectMany(t => t.Methods), "InjectedAttribute"))
            {
                var targetType = MemberMap.GetTargetType(method.DeclaringType);
                MemberMap.AddMethod(method, m_methodPatcher.InjectMethod(method, targetType));
            }
        }

        private void InjectFields()
        {
            foreach (var field in FindMembersWithAttribute(SourceModule.Types.SelectMany(t => t.Fields), "InjectedAttribute"))
            {
                var targetType = MemberMap.GetTargetType(field.DeclaringType);
                var injectedField = new FieldDefinition(field.Name, field.Attributes, MemberMap.ConvertType(field.FieldType));
                targetType.Fields.Add(injectedField);
                MemberMap.AddField(field, injectedField);
            }
        }

        private static IEnumerable<T> FindMembersWithAttribute<T>(IEnumerable<T> members, string attributeName) where T : Mono.Cecil.ICustomAttributeProvider
        {
            return members.Where(m => m.CustomAttributes.Any(attr => attr.AttributeType.Name == attributeName));
        }
    }
}
