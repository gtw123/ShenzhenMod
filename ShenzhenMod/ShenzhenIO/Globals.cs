using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ShenzhenMod.Patching;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByMethod("LocateType")]
    public static class Globals
    {
        [ResolveByMethod("LocateMaxBoardSize")]
        public static Index2 MaxBoardSize;

        [ResolveByMethod("LocateSaveDirectory")]
        public static string SaveDirectory;

        [ResolveByType]
        public static SortedDictionary<string, LcdPattern> CustomScreens;


        public static TypeDefinition LocateType(ModuleDefinition module)
        {
            // The Solution constructor has a line like this:
            //     this.someField = new HashSet<int>[Globals.MaxTestRuns];
            // We use this to find the "Globals" type.
            var ctor = module.FindMethod("Solution", ".ctor");
            var instr = ctor.Body.Instructions.Single(i => i.OpCode == OpCodes.Newarr && i.Operand?.ToString() == "System.Collections.Generic.HashSet`1<System.Int32>");
            return ((FieldDefinition)instr.Previous.Operand).DeclaringType;
        }

        public static FieldDefinition LocateMaxBoardSize(TypeDefinition type)
        {
            // Find "MaxBoardSize = new Index2(22, 14)" and use that to get the MaxBoardSize field
            return (FieldDefinition)type.FindMethod(".cctor").FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).FindNext(OpCodes.Stsfld).Operand;
        }

        public static FieldDefinition LocateSaveDirectory(TypeDefinition type)
        {
            // SaveDirectory is referenced from SolutionManager.GetSolutionFileName
            var solutionType = type.Module.GetType("Solution");
            var solutionManager = type.Module.Types.Single(t => t.Methods.Any(m => m.ReturnType == solutionType && m.Parameters.Count == 1 && m.Parameters[0].ParameterType == solutionType));
            var getSolutionFileName = solutionManager.Methods.Single(m => m.ReturnType == type.Module.TypeSystem.String && m.Parameters.Count == 1 && m.Parameters[0].ParameterType == solutionType);
            return (FieldDefinition)getSolutionFileName.Body.Instructions.Single(i => i.OpCode == OpCodes.Ldsfld && ((FieldReference)i.Operand).DeclaringType == type).Operand;
        }

    }
}
