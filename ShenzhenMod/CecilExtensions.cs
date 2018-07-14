using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod
{
    public static class CecilExtensions
    {
        public static FieldDefinition FindField(this ModuleDefinition module, string typeName, string fieldName)
        {
            var type = module.FindType(typeName);
            return type.Fields.SingleOrDefault(m => m.Name == fieldName) ?? throw new Exception($"Cannot find field \"{fieldName}\" in type \"{type.Name}\"");
        }

        public static MethodDefinition FindMethod(this ModuleDefinition module, string typeName, string methodName)
        {
            return module.FindType(typeName).FindMethod(methodName);
        }

        public static MethodDefinition FindMethod(this TypeDefinition type, string methodName)
        {
            var method = type.Methods.Where(m => m.Name == methodName);
            if (method.Count() == 0)
            {
                throw new Exception($"Cannot find method \"{methodName}\" in type \"{type.Name}\"");
            }
            else if (method.Count() > 1)
            {
                throw new Exception($"Found more than one method called \"{methodName}\" in type \"{type.Name}\"");
            }

            return method.First();
        }

        public static TypeDefinition FindType(this ModuleDefinition module, string name)
        {
            return module.GetType(name) ?? throw new Exception($"Cannot find type \"{name}\"");
        }

        public static Instruction FindInstructionAtOffset(this MethodDefinition method, int offset, OpCode opCode, object operand)
        {
            var instr = method.Body.Instructions.SingleOrDefault(i => i.Offset == offset) ?? throw new Exception($"Cannot find instruction at offset IL_{offset:X4} in method \"{method.Name}\"");
            if (!instr.Matches(opCode, operand))
            {
                throw new Exception($"Instruction at offset IL_{offset:X4} in method \"{method.Name}\" does not match OpCode \"{opCode}\" and operand \"{operand}\"");
            }

            return instr;
        }

        public static IEnumerable<Instruction> FindInstructions(this MethodDefinition method, OpCode opCode, object operand, int numExpected)
        {
            var instr = FindInstructions(method, opCode, operand);
            if (instr.Count() != numExpected)
            {
                throw new Exception($"Expected to find {numExpected} instructions with OpCode \"{opCode}\" and operand \"{operand}\" in method \"{method.Name}\", but found {instr.Count()}");
            }

            return instr;
        }

        public static IEnumerable<Instruction> FindInstructions(this MethodDefinition method, OpCode opCode, object operand)
        {
            return method.Body.Instructions.Where(i => i.Matches(opCode, operand));
        }

        public static Instruction FindInstruction(this MethodDefinition method, OpCode opCode, object operand)
        {
            var instr = FindInstructions(method, opCode, operand);
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

        public static bool Matches(this Instruction instruction, OpCode opCode, object operand)
        {
            return instruction.OpCode == opCode && Object.Equals(instruction.Operand, operand);
        }

        public static void Set(this Instruction instruction, OpCode opCode, object operand)
        {
            instruction.OpCode = opCode;
            instruction.Operand = operand;
        }

        public static IEnumerable<Instruction> RemoveRange(this ILProcessor il, Instruction start, Instruction end)
        {
            var removed = new List<Instruction>();
            var current = start;
            while (current != end)
            {
                var next = current.Next;
                il.Remove(current);
                removed.Add(current);
                current = next;
            }

            return removed;
        }

        public static void InsertBefore(this ILProcessor il, Instruction target, IEnumerable<Instruction> instructions)
        {
            foreach (var instr in instructions)
            {
                il.InsertBefore(target, instr);
            }
        }
    }
}