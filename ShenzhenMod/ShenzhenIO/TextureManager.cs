using System.Linq;
using Mono.Cecil;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByMethod("LocateType")]
    public class TextureManager
    {
        public static TypeDefinition LocateType(ModuleDefinition module) => module.Types.Single(t => t.Fields.Count > 24 && t.Fields.Take(10).All(f => f.FieldType.Name == "Texture"));
    }
}
