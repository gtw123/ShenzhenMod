using System;
using System.Collections.Generic;
using Mono.Cecil.Cil;

namespace ShenzhenMod
{
    public static class CecilCilExtensions
    {
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

        public static void InsertRangeBefore(this ILProcessor il, Instruction target, IEnumerable<Instruction> instructions)
        {
            foreach (var instr in instructions)
            {
                il.InsertBefore(target, instr);
            }
        }

        public static void InsertBefore(this ILProcessor il, Instruction target, params Instruction[] instructions)
        {
            InsertRangeBefore(il, target, instructions);
        }
    }
}