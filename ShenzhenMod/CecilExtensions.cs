using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod
{
    public static class CecilExtensions
    {
        public static TypeDefinition FindType(this ModuleDefinition module, string name)
        {
            return module.GetType(name) ?? throw new Exception($"Cannot find type \"{name}\"");
        }

        public static MethodDefinition FindMethod(this ModuleDefinition module, string typeName, string methodName)
        {
            var type = FindType(module, typeName);
            var method = type.Methods.Where(m => m.Name == methodName);
            if (method.Count() == 0)
            {
                throw new Exception($"Cannot find method \"{methodName}\" in type \"{typeName}\"");
            }
            else if (method.Count() > 1)
            {
                throw new Exception($"Found more than one method called \"{methodName}\" in type \"{typeName}\"");
            }

            return method.First();
        }

        public static Instruction FindInstruction(this MethodDefinition method, OpCode opCode, object operand)
        {
            var instr = method.Body.Instructions.Where(i => i.OpCode == opCode && i.Operand.Equals(operand));

            if (instr.Count() == 0)
            {
                throw new Exception($"Cannot find instruction with OpCode \"{opCode}\" and operand \"{operand}\" in method \"{method.Name}\"");
            }
            else if (instr.Count() > 1)
            {
                throw new Exception($"Found more than one instruction with OpCode \"{opCode}\" and operand \"{operand}\" in method \"{method.Name}\"");
            }

            return instr.First();
        }
    }
}