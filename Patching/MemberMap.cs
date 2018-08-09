using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using static System.FormattableString;

namespace ShenzhenMod.Patching
{
    public class MemberMap
    {
        private class MemberReferenceComparer : IEqualityComparer<MemberReference>
        {
            public bool Equals(MemberReference x, MemberReference y)
            {
                return x.FullName == y.FullName;
            }

            public int GetHashCode(MemberReference obj)
            {
                return obj.FullName.GetHashCode();
            }
        }

        private readonly Dictionary<MemberReference, TypeDefinition> m_types = new Dictionary<MemberReference, TypeDefinition>(new MemberReferenceComparer());
        private readonly Dictionary<MemberReference, MethodDefinition> m_methods = new Dictionary<MemberReference, MethodDefinition>(new MemberReferenceComparer());
        private readonly Dictionary<MemberReference, FieldDefinition> m_fields = new Dictionary<MemberReference, FieldDefinition>(new MemberReferenceComparer());

        public ModuleDefinition SourceModule { get; private set; }
        public ModuleDefinition TargetModule { get; private set; }

        public MemberMap(ModuleDefinition sourceModule, ModuleDefinition targetModule)
        {
            SourceModule = sourceModule;
            TargetModule = targetModule;
        }

        public void AddType(TypeReference source, TypeDefinition target)
        {
            AssertSourceModule(source, "Source type");
            AssertTargetModule(target, "Target type");

            m_types[source] = target;
        }

        public void AddMethod(MethodReference source, MethodDefinition target)
        {
            AssertSourceModule(source, "Source method");
            AssertTargetModule(target, "Target method");

            m_methods[source] = target;
        }

        public void AddField(FieldReference source, FieldDefinition target)
        {
            AssertSourceModule(source, "Source field");
            AssertTargetModule(target, "Target field");

            m_fields[source] = target;
        }

        public TypeDefinition GetTargetType(string typeName)
        {
            return GetTargetType(SourceModule.FindType(typeName));
        }

        public TypeDefinition GetTargetType(TypeReference sourceType)
        {
            return m_types.GetValueOrNull(sourceType) ?? throw new Exception(Invariant($"Can't find substitute type for \"{sourceType.Name}\""));
        }

        public MethodDefinition GetTargetMethod(string typeName, string methodName)
        {
            return GetTargetMethod(SourceModule.FindMethod(typeName, methodName));
        }

        public MethodDefinition GetTargetMethod(MethodReference sourceMethod)
        {
            return m_methods.GetValueOrNull(sourceMethod) ?? throw new Exception(Invariant($"Can't find substitute method for \"{sourceMethod.Name}\""));
        }
        
        public FieldDefinition GetTargetField(string typeName, string fieldName)
        {
            return GetTargetField(SourceModule.FindField(typeName, fieldName));
        }

        public FieldDefinition GetTargetField(FieldReference sourceField)
        {
            return m_fields.GetValueOrNull(sourceField) ?? throw new Exception(Invariant($"Can't find substitute field for \"{sourceField.Name}\""));
        }

        public MethodReference ConvertMethod(MethodReference method)
        {
            if (method is GenericInstanceMethod genericMethod)
            {
                return ConvertGenericInstanceMethod(genericMethod);
            }
            
            var methodToConvert = method;
            if (method.DeclaringType is GenericInstanceType)
            {
                methodToConvert = method.RemoveGenericArguments();
            }

            var convertedMethod = m_methods.GetValueOrNull(methodToConvert) ?? methodToConvert;

            if (method.DeclaringType is GenericInstanceType genericType)
            {
                convertedMethod = convertedMethod.MakeGenericInstance(genericType.GenericArguments.Select(arg => ConvertType(arg)).ToArray());
            }

            AssertConvertedTypeNotInSourceModule(convertedMethod.DeclaringType);

            return TargetModule.ImportReference(convertedMethod);
        }

        private MethodReference ConvertGenericInstanceMethod(GenericInstanceMethod method)
        {
            var convertedMethod = new GenericInstanceMethod(ConvertMethod(method.ElementMethod));
            foreach (var genericArgument in method.GenericArguments)
            {
                convertedMethod.GenericArguments.Add(ConvertType(genericArgument));
            }

            return convertedMethod;
        }

        public FieldReference ConvertField(FieldReference field)
        {
            var convertedField = m_fields.GetValueOrNull(field) ?? field;
            AssertConvertedTypeNotInSourceModule(convertedField.DeclaringType);
            AssertConvertedTypeNotInSourceModule(convertedField.FieldType);

            return TargetModule.ImportReference(convertedField);
        }

        public TypeReference ConvertType(TypeReference type)
        {
            if (type is GenericInstanceType genericType)
            {
                return ConvertGenericType(genericType);
            }
            else if (type is GenericParameter genericParam)
            {
                return ConvertGenericParameter(genericParam);
            }
            else if (type is ArrayType arrayType)
            {
                return ConvertArrayType(arrayType);
            }

            var convertedType = m_types.GetValueOrNull(type) ?? type;

            AssertConvertedTypeNotInSourceModule(convertedType);

            return TargetModule.ImportReference(convertedType);
        }

        private TypeReference ConvertGenericType(GenericInstanceType type)
        {
            var elemType = ConvertType(type.ElementType);
            var newType = elemType.MakeGenericInstanceType(type.GenericArguments.Select(arg => ConvertType(arg)).ToArray());
            return TargetModule.ImportReference(newType);
        }

        private TypeReference ConvertGenericParameter(GenericParameter param)
        {
            if (param.DeclaringType != null)
            {
                return ConvertType(param.DeclaringType).GenericParameters[param.Position];
            }
            else if (param.DeclaringMethod != null)
            {
                return ConvertMethod(param.DeclaringMethod).GenericParameters[param.Position];
            }
            else
            {
                throw new Exception(Invariant($"Generic parameter \"{param.Name}\" has no declaring type or method"));
            }
        }

        private TypeReference ConvertArrayType(ArrayType type)
        {
            var newType = ConvertType(type.ElementType).MakeArrayType();
            return TargetModule.ImportReference(newType);
        }

        private void AssertSourceModule(MemberReference member, string name)
        {
            if (member.Module != SourceModule)
            {
                throw new Exception(Invariant($"{name} does not belong to source module"));
            }
        }

        private void AssertTargetModule(MemberReference member, string name)
        {
            if (member.Module != TargetModule)
            {
                throw new Exception(Invariant($"{name} does not belong to target module"));
            }
        }

        private void AssertConvertedTypeNotInSourceModule(TypeReference type)
        {
            // TODO: Is this check still working?

            // We expect all types from m_sourceModule's assembly to be converted to types in another assembly.
            // We don't care about types from external assemblies (e.g. mscorlib.dll) though.
            if (type.Scope.Name == SourceModule.Assembly.MainModule.Name)
            {
                throw new Exception(Invariant($"Could not find an appropriate type to convert \"{type}\" to"));
            }
        }
    }
}
