using System.Linq;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByMethod("LocateType", Class = "ShenzhenIO.OptionalLocator")]
    public struct Optional<T>
    {
        [ResolveBySignature]
        public bool HasValue()
        {
            return false;
        }

        [ResolveBySignature]
        public T GetValue()
        {
            return default(T);
        }
    }

    // This needs to be on a separate class because Optional has generic parameters.
    public class OptionalLocator
    {
        public static TypeDefinition LocateType(ModuleDefinition module) => module.Types.Single(t => t.IsValueType && t.HasGenericParameters && t.Fields.Count == 2
            && t.Fields[0].FieldType == module.TypeSystem.Boolean && t.Fields[1].FieldType == t.GenericParameters[0]);
    }
}
