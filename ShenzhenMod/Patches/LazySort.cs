using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using static System.FormattableString;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Patches the code to make solution chip list sorting lazy.
    public class LazySort
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(LazySort));

        private ShenzhenTypes m_types;

        public LazySort(ShenzhenTypes types)
        {
            m_types = types;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            PatchSort();
        }

        /// <summary>
        /// Patches the code that sorts the list of Chips to be lazy (only sort if needed).
        /// Sorting the list causes a `Collection was modified` exception to be thrown.
        /// </summary>
        private void PatchSort()
        {
            // Find the method that sorts the list of chips
            var method = m_types.Solution.Type.Methods.Single(m => !m.IsPublic && !m.IsConstructor && !m.IsStatic && m.Parameters.Count == 0 && m.ReturnType == m_types.BuiltIn.Void);
            var il = method.Body.GetILProcessor();
            method.Body.SimplifyMacros();

            /* Replace this:

                this.chipList.Sort((chip1, chip2) => { chip1.Index.CompareTo(chip2.Index); });

            with this:

                var comparison = new Comparison<Chip>((chip1, chip2) => { chip1.Index.CompareTo(chip2.Index); });
                for (int i = 1; i < this.chipList.Count; i++)
                {
                    if (comparison.Compare(this.chipList[i - 1], this.chipList[i]) > 0)
                    {
                        this.chipList.Sort(comparison);
                        return;
                    }
                }

            */

            var instructions = method.Body.Instructions;
            var loadChipList = instructions[1];
            if (loadChipList.OpCode != OpCodes.Ldfld)
            {
                throw new Exception(Invariant($"Unexpected instruction, wanted: ldfld, found: {loadChipList.OpCode}"));
            }
            var callSort = instructions[instructions.Count - 2];
            if (callSort.OpCode != OpCodes.Callvirt)
            {
                throw new Exception(Invariant($"Unexpected instruction, wanted: callvirt, found: {callSort.OpCode}"));
            }

            var listChipType = m_types.Module.ImportReference(typeof(List<>)).MakeGenericInstanceType(m_types.Chip.Type);
            var comparisonChipType = m_types.Module.ImportReference(typeof(Comparison<>)).MakeGenericInstanceType(m_types.Chip.Type);

            var lbl_static_init_end = il.Create(OpCodes.Nop);
            method.FindInstruction(OpCodes.Brtrue, callSort).Operand = lbl_static_init_end;

            var comparison = new VariableDefinition(comparisonChipType);
            method.Body.Variables.Add(comparison);

            var sortAndReturn = new List<Instruction>();
            // ldarg.0; ldfld List<Chip> Solution::chipList
            sortAndReturn.AddRange(il.RemoveRange(
                method.FindInstruction(OpCodes.Ldarg, il.Create(OpCodes.Ldarg, 0).Operand),
                loadChipList.Next));
            sortAndReturn.Add(il.Create(OpCodes.Ldloc, comparison));
            // callVirt List<Chip>::Sort(Comparison<Chip>); ret
            sortAndReturn.AddRange(il.RemoveRange(callSort, null));

            il.Append(lbl_static_init_end);
            il.Emit(OpCodes.Stloc, comparison);

            // for (int i = 1; i < this.chipList.Count; i++)...
            var i = new VariableDefinition(m_types.BuiltIn.Int32);
            method.Body.Variables.Add(i);
            // int i = 1
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Stloc, i);
            var lbl_for_start = il.Create(OpCodes.Nop);
            var lbl_for_end = il.Create(OpCodes.Nop);
            il.Append(lbl_for_start);
            // i < this.chipList.Count
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, (FieldDefinition)loadChipList.Operand);
            il.Emit(OpCodes.Callvirt, m_types.Module.ImportReference(listChipType.Resolve().FindMethod("get_Count").MakeGeneric(m_types.Chip.Type)));
            il.Emit(OpCodes.Clt);
            il.Emit(OpCodes.Brfalse, lbl_for_end);
            {
                // if (comparison.Compare(this.chipList[i - 1], this.chipList[i]) > 0)...
                il.Emit(OpCodes.Ldloc, comparison);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, (FieldDefinition)loadChipList.Operand);
                il.Emit(OpCodes.Ldloc, i);
                il.Emit(OpCodes.Ldc_I4, 1);
                il.Emit(OpCodes.Sub);
                il.Emit(OpCodes.Callvirt, m_types.Module.ImportReference(listChipType.Resolve().FindMethod("get_Item").MakeGeneric(m_types.Chip.Type)));
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, (FieldDefinition)loadChipList.Operand);
                il.Emit(OpCodes.Ldloc, i);
                il.Emit(OpCodes.Callvirt, m_types.Module.ImportReference(listChipType.Resolve().FindMethod("get_Item").MakeGeneric(m_types.Chip.Type)));
                il.Emit(OpCodes.Callvirt, m_types.Module.ImportReference(comparisonChipType.Resolve().FindMethod("Invoke").MakeGeneric(m_types.Chip.Type)));
                il.Emit(OpCodes.Ldc_I4, 0);
                il.Emit(OpCodes.Cgt);
                var lbl_endif = il.Create(OpCodes.Nop);
                il.Emit(OpCodes.Brfalse, lbl_endif);
                {
                    // this.chipList.Sort(comparison);
                    // return;
                    sortAndReturn.ForEach(il.Append);

                    il.Append(lbl_endif);
                }
                // end if (if (comparison.Compare(this.chipList[i - 1], this.chipList[i]) > 0)...)

                // i++
                il.Emit(OpCodes.Ldloc, i);
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, i);
                il.Emit(OpCodes.Pop);
                il.Emit(OpCodes.Br, lbl_for_start);
                il.Append(lbl_for_end);
            } // end for (for (int i = 1; i < this.chipList.Count; i++)...)

            // return;
            il.Emit(OpCodes.Ret);

            method.Body.OptimizeMacros();
        }
    }
}
