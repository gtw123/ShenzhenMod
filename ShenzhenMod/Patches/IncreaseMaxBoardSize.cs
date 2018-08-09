using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using ShenzhenMod.Patching;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Patches the code to increase the maximum circuit board size.
    /// </summary>
    public class IncreaseMaxBoardSize
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(IncreaseMaxBoardSize));

        private ShenzhenTypes m_types;

        public IncreaseMaxBoardSize(ShenzhenTypes types)
        {
            m_types = types;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            ChangeMaxBoardSize();
            ChangeScrollSize();
            AddSizeToPuzzle();
            PatchTileCreation();
            PatchTraceReading();
            PatchTraceWriting();
            PatchCustomPuzzleReading();
        }

        /// <summary>
        /// Changes the maximum circuit board size to a bigger size, to allow bigger puzzles.
        /// </summary>
        private void ChangeMaxBoardSize()
        {
            // Find "MaxBoardSize = new Index2(22, 14)" and change the values.
            var instr = m_types.Globals.ClassConstructor.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)22);
            instr.Operand = (sbyte)44;
            instr.Next.Operand = (sbyte)28;
        }

        /// <summary>
        /// Changes the window size at which scrolling will be disabled. By default this
        /// is 1920x1080 because that's big enough for the largest built-in puzzles. We
        /// need to increase so we can have bigger puzzles. (Ideally we could find a way
        /// to make this be dynamically adjusted based on the current puzzle and the
        /// window size, but this is good enough for now.)
        /// </summary>
        private void ChangeScrollSize()
        {
            // Find the method which returns a bool and uses the constant 1920f
            var method = m_types.Globals.Type.Methods.Where(m => m.IsPublic && m.IsStatic && m.ReturnType == m_types.BuiltIn.Boolean)
                .Single(m => m.Body.Instructions.Any(i => i.Matches(OpCodes.Ldc_R4, 1920f)));
            method.FindInstruction(OpCodes.Ldc_R4, 1920f).Operand = 3840f;
            method.FindInstruction(OpCodes.Ldc_R4, 1080f).Operand = 2160f;
        }

        /// <summary>
        /// Adds fields to the Puzzle class to store its actual board size, and accessors for the board size.
        /// </summary>
        private void AddSizeToPuzzle()
        {
            var puzzleType = m_types.Puzzle.Type;
            var sizeField = new FieldDefinition("size", FieldAttributes.Private, m_types.Index2.Type);
            puzzleType.Fields.Add(sizeField);
            var isSizeSetField = new FieldDefinition("isSizeSet", FieldAttributes.Private, m_types.BuiltIn.Boolean);
            puzzleType.Fields.Add(isSizeSetField);

            AddSetSize();
            AddGetSize();

            void AddSetSize()
            {
                var method = new MethodDefinition("SetSize", MethodAttributes.Public, m_types.BuiltIn.Void);
                method.Parameters.Add(new ParameterDefinition("size", ParameterAttributes.None, m_types.Index2.Type));
                puzzleType.Methods.Add(method);
                var il = method.Body.GetILProcessor();

                // this.size = size
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Stfld, sizeField);
                
                // isSizeSet = true
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stfld, isSizeSetField);

                il.Emit(OpCodes.Ret);
            }

            // Adds GetSize() to the Puzzle class.
            // This returns the size of the board if it has been set via SetSize(), and
            // defaults to (22, 14) if not. This is to avoid having to update all the existing
            // puzzles to set their size.
            void AddGetSize()
            {
                var method = new MethodDefinition("GetSize", MethodAttributes.Public, m_types.Index2.Type);
                puzzleType.Methods.Add(method);
                var il = method.Body.GetILProcessor();
                var label1 = il.Create(OpCodes.Nop);
                
                // if (isSizeSet)
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, isSizeSetField);
                il.Emit(OpCodes.Brfalse_S, label1);

                // return size
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, sizeField);
                il.Emit(OpCodes.Ret);

                // return new Index2(22, 14)
                il.Append(label1);
                label1.Set(OpCodes.Ldc_I4_S, (sbyte)22);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)14);
                il.Emit(OpCodes.Newobj, m_types.Index2.Constructor);

                il.Emit(OpCodes.Ret);
            }
        }

        /// <summary>
        /// Patches the code that populates the tiles of the circuit board from the puzzle definition
        /// so that it uses the actual board size of the puzzle rather than the maximum board size.
        /// This allows us to add puzzles with custom board sizes without needing to change the size of
        /// existing puzzles.
        /// </summary>
        private void PatchTileCreation()
        {
            // This isn't a particularly intuitive way to find this class, but it's good enough for now
            var type = m_types.Module.Types.Single(t => t.Fields.Count == 0 && t.Methods.Any(m => m.ReturnType.ToString() == "Texture[]"));

            var method = type.Methods.Single(m => m.IsPublic && m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType == m_types.Puzzle.Type);
            var il = method.Body.GetILProcessor();

            var instructionsToReplace = method.FindInstructions(OpCodes.Ldsfld, m_types.Globals.MaxBoardSize);
            foreach (var instr in instructionsToReplace.ToList())
            {
                il.InsertBefore(instr, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(instr, il.Create(OpCodes.Call, m_types.Puzzle.Type.FindMethod("GetSize")));
                il.Remove(instr);
            }
        }

        /// <summary>
        /// Patches the code that reads traces from a solution file so that it works if the
        /// puzzle has a smaller board size than the maximum board size.
        /// </summary>
        private void PatchTraceReading()
        {
            var method = m_types.Solution.Type.Methods.Single(m => m.IsPrivate && m.Parameters.Count == 2
                && m.Parameters[0].ParameterType.ToString().EndsWith("<Chip>&"));
            var il = method.Body.GetILProcessor();

            FixRowShuffling();
            FixRowReading();

            // Fix up branches instructions that can no longer be "short" branches
            method.Body.SimplifyMacros();
            method.Body.OptimizeMacros();

            // In memory, the traces are stored in reverse order to the solution file - i.e. row 0
            // is actually at the bottom of the screen. This means that when the traces are read in
            // from the solution file, they are inserted into the traces "grid" in reverse row order.
            // If there are less rows in the solution file than the max puzzle height, the rows need
            // to be shuffled down so that the last row is at index 0. By default, the code only
            // handles this for 22x11 and 22x14. We patch it so that it works for any number of rows.
            void FixRowShuffling()
            {
                /* Replace this:
                 
			        if (index == new Index2(0, 2))
			        {
				        for (int j = 0; j < 22; j++)
				        {
					        for (int k = 0; k < 11; k++)
					        {
						        this.Traces.AddTrace(new Index2(j, k), this.Traces.GetTrace(new Index2(j, k + 3)));
					        }
					        for (int l = 11; l < 14; l++)
					        {
						        this.Traces.AddTrace(new Index2(j, l), this.Trace.None);
					        }
				        }
        			}

                with this:

                    int lastY = index.Y;
                    if (lastY > 0)
                    {
                        int maxY = this.Traces.GetSize().Y;
                        for (int j = 0; j < this.Traces.GetSize().X; j++)
                        {
                            for (int k = 0; k < maxY - lastY; k++)
                            {
                                this.Traces.AddTrace(new Index2(j, k), this.Traces.GetTrace(new Index2(j, k + lastY)));
                            }
                            for (int l = maxY - lastY; l < maxY; l++)
                            {
                                this.Traces.AddTrace(new Index2(j, l), Trace.None);
                            }
                        }
                    }

                 */

                // Remove the "if (index == new Index2(0, 2))" check, but leave the first instruction so we don't break branches
                var ifIndexEquals02 = method.FindInstructionAtOffset(0x04be, OpCodes.Ldloc_S, method.Body.Variables[13]);
                var jLoopStart = method.FindInstructionAtOffset(0x04ce, OpCodes.Ldc_I4_0, null);
                il.RemoveRange(ifIndexEquals02.Next, jLoopStart);

                // Add two new variables
                var lastY = new VariableDefinition(m_types.BuiltIn.Int32);
                method.Body.Variables.Add(lastY);
                var maxY = new VariableDefinition(m_types.BuiltIn.Int32);
                method.Body.Variables.Add(maxY);

                // Initialize the two new variables and add the "if (lastY > 0)" check
                ifIndexEquals02.Set(OpCodes.Ldloca_S, method.Body.Variables[13]);
                il.InsertBefore(jLoopStart,
                    il.Create(OpCodes.Ldfld, m_types.Index2.Y),
                    il.Create(OpCodes.Stloc_S, lastY),
                    il.Create(OpCodes.Ldloc_S, lastY),
                    il.Create(OpCodes.Ldc_I4_0),
                    il.Create(OpCodes.Ble, method.Body.Instructions.Last()),
                    il.Create(OpCodes.Ldarg_0),
                    il.Create(OpCodes.Ldfld, m_types.Solution.Traces),
                    il.Create(OpCodes.Callvirt, m_types.Traces.GetSize),
                    il.Create(OpCodes.Ldfld, m_types.Index2.Y),
                    il.Create(OpCodes.Stloc_S, maxY));

                // Replace "k + 3" with "k + lastY"
                method.FindInstruction(OpCodes.Ldc_I4_3, null).Set(OpCodes.Ldloc_S, lastY);

                // Replace "l < 14" with "l < maxY"
                method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).Set(OpCodes.Ldloc_S, maxY);

                // Replace "11" with "maxY - lastY"
                foreach (var instr in method.FindInstructions(OpCodes.Ldc_I4_S, (sbyte)11, 2).ToList())
                {
                    instr.Set(OpCodes.Ldloc_S, maxY);
                    il.InsertBefore(instr.Next,
                        il.Create(OpCodes.Ldloc_S, lastY),
                        il.Create(OpCodes.Sub));
                }

                // Replace "22" with "this.Traces.GetSize().X"
                var instr2 = method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)22);
                instr2.Set(OpCodes.Ldarg_0, null);
                il.InsertBefore(instr2.Next,
                    il.Create(OpCodes.Ldfld, m_types.Solution.Traces),
                    il.Create(OpCodes.Callvirt, m_types.Traces.GetSize),
                    il.Create(OpCodes.Ldfld, m_types.Index2.X));
            }

            // By default, the trace reading code assumes the width of the traces in the solution file
            // is the same as the max board width. We change it so that it uses the width of the rows
            // in the solution file instead. This allows it to correctly read (say) a 22x14 solution
            // if the max board size is bigger than 22x14.
            void FixRowReading()
            {
                /* Replace this:

                    if (flag)
                    {
                        index2.x++;
                        if (index2.x >= this.Traces.GetSize().x)
                        {
                            index2.x = 0;
                            index2.y--;
                            if (index2.y < 0)
                            {
                                break;
                            }
                        }
                    }

                with this:

                    else if (chr == '\n')
                    {
                        index2.x = 0;
                        index2.y--;
                        if (index2.y < 0)
                        {
                            break;
                        }
                    }
                    if (flag)
                    {
                        index2.x++;
                    }
                */

                var ifFlag = method.FindInstructionAtOffset(0x0463, OpCodes.Ldloc_S, method.Body.Variables[16]);
                var ifXGreater = method.FindInstructionAtOffset(0x0473, OpCodes.Ldloc_S, method.Body.Variables[13]);
                var handleEOL = method.FindInstructionAtOffset(0x048c, OpCodes.Ldloca_S, method.Body.Variables[13]);
                var loopBodyEnd = method.FindInstructionAtOffset(0x04aa, OpCodes.Ldloc_S, method.Body.Variables[4]);

                // If the if before "if (flag)" is true, jump to the "if (flag" rather than falling through
                il.InsertBefore(ifFlag, il.Create(OpCodes.Br, ifFlag));

                // Remove some of the instructions from the "if (flag)" block and shift some of them towards the bottom of the loop
                var removed = il.RemoveRange(ifFlag, handleEOL);
                il.InsertRangeBefore(loopBodyEnd, removed.TakeWhile(i => i != ifXGreater));

                // Insert the '\n' check
                var checkNewLine = il.Create(OpCodes.Ldloc_S, method.Body.Variables[15]);
                il.InsertBefore(handleEOL,
                    checkNewLine,
                    il.Create(OpCodes.Ldc_I4_S, (sbyte)10),
                    il.Create(OpCodes.Bne_Un, ifFlag));

                // If the if before "if (flag)" is false, jump to our new '\n' check
                method.FindInstructionAtOffset(0x044b, OpCodes.Brfalse_S, ifFlag).Operand = checkNewLine;
            }
        }

        /// <summary>
        /// Patches the code that writes traces to a solution file so that it uses the board size of
        /// the puzzle rather than the maximum board size. This ensures that solutions for
        /// default puzzles are written out the same way even after we've increased the maximum
        /// board size.
        /// </summary>
        private void PatchTraceWriting()
        {
            // Look for the private method with a single StringBuilder parameter
            var method = m_types.Solution.Type.Methods.Single(m => m.IsPrivate && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.ToString() == "System.Text.StringBuilder");
            var il = method.Body.GetILProcessor();

            /* Replace this:

                for (int i = this.Traces.GetSize().Y - 1; i >= 0; i--)
                {
                    for (int j = 0; j < this.Traces.GetSize().X; j++)

            with this:

                Index2 size = this.Traces.GetSize();
                Optional<Puzzle> puzzle = Puzzles.FindPuzzle(this.PuzzleName);
                if (puzzle.HasValue())
                {
                    size = puzzle.Value().GetSize();
                }
                for (int i = size.int_1 - 1; i >= 0; i--)
                {
                    for (int j = 0; j < size.int_0; j++)

            */

            var puzzleType = m_types.Puzzle.Type;
            var solutionType = m_types.Solution.Type;

            // Add two new variables
            var size = new VariableDefinition(m_types.Index2.Type);
            method.Body.Variables.Add(size);
            var puzzle = new VariableDefinition(m_types.Optional.Type.MakeGenericInstanceType(puzzleType));
            method.Body.Variables.Add(puzzle);

            var loopStart = method.FindInstructionAtOffset(0x0010, OpCodes.Ldarg_0, null);

            il.InsertBefore(loopStart,
                // size = this.Traces.GetSize();
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, m_types.Solution.Traces),
                il.Create(OpCodes.Callvirt, m_types.Traces.GetSize),
                il.Create(OpCodes.Stloc_S, size),

                // Optional<Puzzle> puzzle = Puzzles.FindPuzzle(this.PuzzleName);
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, m_types.Solution.PuzzleName),
                il.Create(OpCodes.Call, m_types.Puzzles.FindPuzzle),
                il.Create(OpCodes.Stloc_S, puzzle),

                // if (puzzle.HasValue())
                il.Create(OpCodes.Ldloca_S, puzzle),
                il.Create(OpCodes.Call, m_types.Optional.HasValue(puzzleType)),
                il.Create(OpCodes.Brfalse_S, loopStart),

                // size = puzzle.GetValue().GetSize();
                il.Create(OpCodes.Ldloca_S, puzzle),
                il.Create(OpCodes.Call, m_types.Optional.GetValue(puzzleType)),
                il.Create(OpCodes.Call, puzzleType.FindMethod("GetSize")),
                il.Create(OpCodes.Stloc_S, size));
            
            // Replace "this.Traces.GetSize()" with "size" in the first loop
            loopStart.Set(OpCodes.Ldloc_S, size);
            il.Remove(loopStart.Next);
            il.Remove(loopStart.Next);

            // Replace "this.Traces.GetSize()" with "size" in the second loop
            var loop2Condition = method.FindInstructionAtOffset(0x005C, OpCodes.Ldarg_0, null);
            loop2Condition.Set(OpCodes.Ldloc_S, size);
            il.Remove(loop2Condition.Next);
            il.Remove(loop2Condition.Next);
        }

        /// <summary>
        /// Patches the code that reads a custom puzzle definition so that it uses the correct board size.
        /// </summary>
        private void PatchCustomPuzzleReading()
        {
            var method = m_types.Module.FindType("CustomLevelCompiler").Methods.Single(m => m.IsPrivate && m.Parameters.Count == 2
                && m.Parameters[0].ParameterType == m_types.BuiltIn.String && m.Parameters[1].ParameterType.ToString() == "System.Collections.Generic.Dictionary`2<System.Char,Index2>");
            var il = method.Body.GetILProcessor();

            // Change the code that gets the max board size to instead use the default board size of 22x14.
            // Technically custom puzzles are always 18x7, but to avoid changing the file format of existing
            // solutions we use the old default of 22x14 instead.
            var instrs = method.FindInstructions(OpCodes.Ldsfld, m_types.Globals.MaxBoardSize, 2).ToList();
            instrs[0].Set(OpCodes.Ldc_I4_S, (sbyte)22);
            il.Remove(instrs[0].Next);

            instrs[1].Set(OpCodes.Ldc_I4_S, (sbyte)14);
            il.Remove(instrs[1].Next);
        }
    }
}
 
 