using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;
using static System.FormattableString;

namespace ShenzhenMod.Patching.Resolvers
{
    public class FieldResolver : MemberResolver
    {
        private struct FieldAttributeInfo<TAttr> where TAttr : Attribute
        {
            public FieldInfo ReflectionField;
            public FieldReference CecilField;
            public TAttr Attribute;
        }

        public FieldResolver(Assembly sourceAssembly, MemberMap memberMap)
            : base(sourceAssembly, memberMap)
        {
        }

        public override void Resolve()
        {
            ResolveByName();
            ResolveByMethod();
            ResolveByPositionAndType();
            ResolveByType();
        }

        private void ResolveByName()
        {
            foreach (var info in FindFieldsWithAttribute<ResolveByNameAttribute>())
            {
                var targetType = MemberMap.GetTargetType(info.CecilField.DeclaringType);
                string targetFieldName = info.Attribute.Name ?? info.ReflectionField.Name;
                var targetField = targetType.FindField(targetFieldName) ??
                    throw new Exception(Invariant($"Cannot locate field named \"{targetFieldName}\" in type \"{targetType.Name}\" in target module"));

                MemberMap.AddField(info.CecilField, targetField);
            }
        }

        private void ResolveByMethod()
        {
            foreach (var info in FindFieldsWithAttribute<ResolveByMethodAttribute>())
            {
                var targetType = MemberMap.GetTargetType(info.CecilField.DeclaringType);
                var targetField = InvokeLocatorMethod<FieldDefinition>(info.Attribute, info.ReflectionField.DeclaringType, targetType);

                MemberMap.AddField(info.CecilField, targetField);
            }
        }

        private void ResolveByPositionAndType()
        {
            foreach (var info in FindFieldsWithAttribute<ResolveByPositionAndTypeAttribute>())
            {
                var field = info.ReflectionField;
                var position = field.DeclaringType.GetRuntimeFields().Where(f => f.FieldType.FullName == field.FieldType.FullName).ToList().IndexOf(field);

                var targetType = MemberMap.GetTargetType(info.CecilField.DeclaringType);

                var targetFieldType = MemberMap.ConvertType(info.CecilField.FieldType);
                // TODO: Give better exception message
                var targetField = targetType.Fields.Where(f => f.FieldType.FullName == targetFieldType.FullName).ToList()[position];
                MemberMap.AddField(info.CecilField, targetField);
            }
        }

        private void ResolveByType()
        {
            foreach (var info in FindFieldsWithAttribute<ResolveByTypeAttribute>())
            {
                var targetType = MemberMap.GetTargetType(info.CecilField.DeclaringType);

                // TODO: Give better exception message if more than one found
                var targetFieldType = MemberMap.ConvertType(info.CecilField.FieldType);
                var targetField = targetType.Fields.Single(f => f.FieldType.FullName == targetFieldType.FullName);
                MemberMap.AddField(info.CecilField, targetField);
            }
        }

        private IEnumerable<FieldAttributeInfo<TAttr>> FindFieldsWithAttribute<TAttr>() where TAttr : Attribute
        {
            return from type in SourceAssembly.GetTypes()
                   from field in type.GetRuntimeFields()
                   from attribute in field.GetCustomAttributes<TAttr>()
                   select new FieldAttributeInfo<TAttr> { ReflectionField = field, CecilField = SourceModule.ImportReference(field), Attribute = attribute };
        }
    }
}
