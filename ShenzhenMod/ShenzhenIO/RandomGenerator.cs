using System.Linq;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByMethod("LocateType")]
    public class RandomGenerator
    {
        public static TypeDefinition LocateType(ModuleDefinition module) => module.Types.Single(t => t.Fields.Count == 2 && t.Fields.All(f => f.FieldType == module.TypeSystem.UInt64));
    }
}
