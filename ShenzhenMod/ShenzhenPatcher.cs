using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ShenzhenMod
{
    public class ShenzhenPatcher : IDisposable
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(ShenzhenPatcher));

        private ModuleDefinition m_module;

        public ShenzhenPatcher(string unpatchedPath)
        {
            sm_log.InfoFormat("Reading module \"{0}\"", unpatchedPath);
            m_module = ModuleDefinition.ReadModule(unpatchedPath);
        }

        public void Dispose()
        {
            if (m_module != null)
            {
                m_module.Dispose();
                m_module = null;
            }
        }

        public void ApplyPatches()
        {
            ChangeMaxBoardSize();
            ChangeScrollSize();
            AddSizeToPuzzle();
            PatchTileCreation();
            PatchTraceReading();
            PatchTraceWriting();
            PatchCustomPuzzleReading();
            AddSandbox2Puzzle();
            AddSandbox2MessageThread();
        }

        /// <summary>
        /// Changes the maximum circuit board size to a bigger size, to allow bigger puzzles.
        /// </summary>
        private void ChangeMaxBoardSize()
        {
            var method = m_module.FindMethod("#=qL_uhZp1CYmicXxDRy$c1Bw==", ".cctor");
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)22).Operand = (sbyte)44;
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).Operand = (sbyte)28;
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
            var method = m_module.FindMethod("#=qL_uhZp1CYmicXxDRy$c1Bw==", "#=qOM$GUjkyhx7zr2YetMuA92qBxgTzT0XpP5Hho_r9c2A=");
            method.FindInstruction(OpCodes.Ldc_R4, 1920f).Operand = 3840f;
            method.FindInstruction(OpCodes.Ldc_R4, 1080f).Operand = 2160f;
        }

        /// <summary>
        /// Adds fields to the Puzzle class to store its actual board size, and accessors for the board size.
        /// </summary>
        private void AddSizeToPuzzle()
        {
            var puzzleType = m_module.FindType("Puzzle");
            var index2DType = m_module.FindType("Index2");
            var sizeField = new FieldDefinition("size", FieldAttributes.Private, index2DType);
            puzzleType.Fields.Add(sizeField);
            var isSizeSetField = new FieldDefinition("isSizeSet", FieldAttributes.Private, m_module.TypeSystem.Boolean);
            puzzleType.Fields.Add(isSizeSetField);

            AddSetSize();
            AddGetSize();

            void AddSetSize()
            {
                var method = new MethodDefinition("SetSize", MethodAttributes.Public, m_module.TypeSystem.Void);
                method.Parameters.Add(new ParameterDefinition("size", ParameterAttributes.None, index2DType));
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
                var method = new MethodDefinition("GetSize", MethodAttributes.Public, index2DType);
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
                il.Emit(OpCodes.Newobj, index2DType.FindMethod(".ctor"));

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
            var method = m_module.FindMethod("#=qWo_Ilq5Sos4EQLLY_xDRAjAw6Q83Z6ZpjGeSwvBaB5U=", "#=qAmFcmFbUxPAB0DUb13KNOEmJgRvZSM6E$AgqIvIwrnk=");
            var il = method.Body.GetILProcessor();

            var instructionsToReplace = method.FindInstructions(OpCodes.Ldsfld, m_module.FindField("#=qL_uhZp1CYmicXxDRy$c1Bw==", "#=qAVzUnhNiHUMBOnnky4iCCA=="), 2);
            foreach (var instr in instructionsToReplace.ToList())
            {
                il.InsertBefore(instr, il.Create(OpCodes.Ldarg_0));
                il.InsertBefore(instr, il.Create(OpCodes.Call, m_module.FindMethod("Puzzle", "GetSize")));
                il.Remove(instr);
            }
        }

        /// <summary>
        /// Patches the code that reads traces from a solution file so that it works if the
        /// puzzle has a smaller board size than the maximum board size.
        /// </summary>
        private void PatchTraceReading()
        {
            var method = m_module.FindMethod("Solution", "#=qMQ0eadM7OBun57pFLO1wWA==");
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
                var lastY = new VariableDefinition(m_module.TypeSystem.Int32);
                method.Body.Variables.Add(lastY);
                var maxY = new VariableDefinition(m_module.TypeSystem.Int32);
                method.Body.Variables.Add(maxY);

                // Initialize the two new variables and add the "if (lastY > 0)" check
                var index2Type = m_module.FindType("Index2");
                ifIndexEquals02.Set(OpCodes.Ldloca_S, method.Body.Variables[13]);
                il.InsertBefore(jLoopStart,
                    il.Create(OpCodes.Ldfld, index2Type.Fields[2]),
                    il.Create(OpCodes.Stloc_S, lastY),
                    il.Create(OpCodes.Ldloc_S, lastY),
                    il.Create(OpCodes.Ldc_I4_0),
                    il.Create(OpCodes.Ble, method.Body.Instructions.Last()),
                    il.Create(OpCodes.Ldarg_0),
                    il.Create(OpCodes.Ldfld, m_module.FindField("Solution", "#=qoToRfupqxh4PHUk11ckgIg==")),
                    il.Create(OpCodes.Callvirt, m_module.FindMethod("#=qU2wvld4wYwd2RmifHjQEOQ==", "#=qa$TS_$HdBzzP07FjV63Yrw==")),
                    il.Create(OpCodes.Ldfld, index2Type.Fields[2]),
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
                    il.Create(OpCodes.Ldfld, m_module.FindField("Solution", "#=qoToRfupqxh4PHUk11ckgIg==")),
                    il.Create(OpCodes.Callvirt, m_module.FindMethod("#=qU2wvld4wYwd2RmifHjQEOQ==", "#=qa$TS_$HdBzzP07FjV63Yrw==")),
                    il.Create(OpCodes.Ldfld, index2Type.Fields[1]));
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
            var method = m_module.FindMethod("Solution", "#=qRifLIKn3UtLp6BE8b6pMlSNVCWp8Sqx23hvNKjNJFiE=");
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

            var index2Type = m_module.FindType("Index2");
            var solutionType = m_module.FindType("Solution");
            var puzzleType = m_module.FindType("Puzzle");
            var optionalType = m_module.FindType("#=qR8Z5w5CojvJHXAww9GCK5A==");

            // Add two new variables
            var size = new VariableDefinition(index2Type);
            method.Body.Variables.Add(size);
            var puzzle = new VariableDefinition(optionalType.MakeGenericInstanceType(puzzleType));
            method.Body.Variables.Add(puzzle);

            var loopStart = method.FindInstructionAtOffset(0x0010, OpCodes.Ldarg_0, null);

            il.InsertBefore(loopStart,
                // size = this.Traces.GetSize();
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, solutionType.FindField("#=qoToRfupqxh4PHUk11ckgIg==")),
                il.Create(OpCodes.Callvirt, m_module.FindMethod("#=qU2wvld4wYwd2RmifHjQEOQ==", "#=qa$TS_$HdBzzP07FjV63Yrw==")),
                il.Create(OpCodes.Stloc_S, size),

                // Optional<Puzzle> puzzle = Puzzles.FindPuzzle(this.PuzzleName);
                il.Create(OpCodes.Ldarg_0),
                il.Create(OpCodes.Ldfld, solutionType.FindField("#=qTXd1K2Ra91bQy9H0YYRImQ==")),
                il.Create(OpCodes.Call, m_module.FindMethod("Puzzles", "#=q3A2LvmLiLWo2_KzmaD5ILQ==")),
                il.Create(OpCodes.Stloc_S, puzzle),

                // if (puzzle.HasValue())
                il.Create(OpCodes.Ldloca_S, puzzle),
                il.Create(OpCodes.Call, optionalType.FindMethod("#=qmSp1X_ZAgy45Ga5$UXTJWQ==").MakeGeneric(puzzleType)),
                il.Create(OpCodes.Brfalse_S, loopStart),

                // size = puzzle.Value().GetSize();
                il.Create(OpCodes.Ldloca_S, puzzle),
                il.Create(OpCodes.Call, optionalType.FindMethod("#=q5w1mvb6GQzXlvbmLqUxfcg==").MakeGeneric(puzzleType)),
                il.Create(OpCodes.Call, m_module.FindMethod("Puzzle", "GetSize")),
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
            var method = m_module.FindMethod("CustomLevelCompiler", "#=qNa$RXaj4bc8c30RVXeX5uQ==");
            var il = method.Body.GetILProcessor();

            // Change the code that gets the max board size to instead use the default board size of 22x14.
            // Technically custom puzzles are always 18x7, but to avoid changing the file format of existing
            // solutions we use the old default of 22x14 instead.
            var instrs = method.FindInstructions(OpCodes.Ldsfld, m_module.FindField("#=qL_uhZp1CYmicXxDRy$c1Bw==", "#=qAVzUnhNiHUMBOnnky4iCCA=="), 2).ToList();
            instrs[0].Set(OpCodes.Ldc_I4_S, (sbyte)22);
            il.Remove(instrs[0].Next);

            instrs[1].Set(OpCodes.Ldc_I4_S, (sbyte)14);
            il.Remove(instrs[1].Next);
        }

        /// <summary>
        /// Adds the Sandbox2 puzzle to the Puzzles class.
        /// </summary>
        private void AddSandbox2Puzzle()
        {
            const int WIDTH = 44;
            const int HEIGHT = 28;

            // Add the static field for the new puzzle
            var puzzlesType = m_module.FindType("Puzzles");
            var sandbox2Field = new FieldDefinition("Sandbox2", FieldAttributes.Static | FieldAttributes.InitOnly, m_module.FindType("Puzzle"));
            puzzlesType.Fields.Add(sandbox2Field);

            // Add the method that creates the puzzle
            var createSandbox2 = AddCreateSandbox2();
            PatchPuzzlesConstructor();

            MethodDefinition AddCreateSandbox2()
            {
                var method = new MethodDefinition("CreateSandbox2", MethodAttributes.Private | MethodAttributes.Static, m_module.TypeSystem.Void);
                puzzlesType.Methods.Add(method);
                var il = method.Body.GetILProcessor();

                var puzzleType = m_module.FindType("Puzzle");
                var puzzle = new VariableDefinition(puzzleType);
                method.Body.Variables.Add(puzzle);

                // Puzzle puzzle = new Puzzle();
                il.Emit(OpCodes.Newobj, puzzleType.FindMethod(".ctor"));
                il.Emit(OpCodes.Stloc_S, puzzle);

                // puzzle.Name = "SzSandbox2";
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldstr, "SzSandbox2");
                il.Emit(OpCodes.Stfld, puzzleType.FindField("#=qlL1QPubmH$dO$NAHNGoUDw=="));

                // puzzle.IsSandbox = Puzzles.Sandbox.IsSandbox;
                var sandboxField = puzzlesType.FindField("#=qvzpyliUbgr777YoqzAAXLGNI$dCHTiOXxvu$zPSifY0=");
                var isSandboxField = puzzleType.FindField("#=qdTVp5wO4TlFm5ZhQQtqszQ==");
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldsfld, sandboxField);
                il.Emit(OpCodes.Ldfld, isSandboxField);
                il.Emit(OpCodes.Stfld, isSandboxField);

                // puzzle.GetTestRunOutputs = Puzzles.Sandbox.GetTestRunOutputs;
                var getTestRunOutputsField = puzzleType.FindField("#=qlGwF$M44YyN9h7pwe8q1VoWLdLS7VaxWT5ZYZgtbLJA=");
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldsfld, sandboxField);
                il.Emit(OpCodes.Ldfld, getTestRunOutputsField);
                il.Emit(OpCodes.Stfld, getTestRunOutputsField);

                // puzzle.Terminals = new Terminal[0];
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newarr, m_module.FindType("Terminal"));
                il.Emit(OpCodes.Stfld, puzzleType.FindField("#=qaFJXQsIwfkO1RLj_cooTHw=="));

                // puzzle.SetSize(new Index2(WIDTH, HEIGHT));
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)WIDTH);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)HEIGHT);
                il.Emit(OpCodes.Newobj, m_module.FindMethod("Index2", ".ctor"));
                il.Emit(OpCodes.Call, puzzleType.FindMethod("SetSize"));

                // puzzle.Tiles = new int[WIDTH * HEIGHT * 3];
                var tilesField = puzzleType.FindField("#=q6aVVxGv0HU7As4f342iIMQ==");
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)WIDTH);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)HEIGHT);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Newarr, m_module.TypeSystem.Int32);
                il.Emit(OpCodes.Stfld, tilesField);

                // It's not clear how to initialize arrays with Mono.Cecil so for now we'll do it
                // the brute force way...

                // Set top row tiles
                SetTile(0, 0, 8, 1);
                SetRowInterior(0, 4, 2);
                SetTile(0, WIDTH - 1, 8, 2);

                // Set middle rows tiles
                for (int row = 1; row < HEIGHT - 1; row++)
                {
                    SetTile(row, 0, 4, 1);
                    SetRowInterior(row, 1, 0);
                    SetTile(row, WIDTH - 1, 4, 3);
                }

                // Set bottom row tiles
                SetTile(HEIGHT - 1, 0, 8, 0);
                SetRowInterior(HEIGHT - 1, 4, 0);
                SetTile(HEIGHT - 1, WIDTH - 1, 8, 3);

                // Puzzles.Sandbox2 = puzzle;
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Stsfld, sandbox2Field);

                il.Emit(OpCodes.Ret);
                return method;

                void SetRowInterior(int row, int value1, int value2)
                {
                    for (int col = 1; col < WIDTH - 1; col++)
                    {
                        SetTile(row, col, value1, value2);
                    }
                }

                void SetTile(int row, int col, int value1, int value2)
                {
                    int index = (row * WIDTH + col) * 3;
                        
                    // puzzle.Tiles[index] = value1;
                    il.Emit(OpCodes.Ldloc_S, puzzle);
                    il.Emit(OpCodes.Ldfld, tilesField);
                    il.Emit(OpCodes.Ldc_I4, index);
                    il.Emit(OpCodes.Ldc_I4_S, (sbyte)value1);
                    il.Emit(OpCodes.Stelem_I4);

                    // puzzle.Tiles[index + 1] = value2;
                    il.Emit(OpCodes.Ldloc_S, puzzle);
                    il.Emit(OpCodes.Ldfld, tilesField);
                    il.Emit(OpCodes.Ldc_I4, index + 1);
                    il.Emit(OpCodes.Ldc_I4_S, (sbyte)value2);
                    il.Emit(OpCodes.Stelem_I4);
                }
            }

            // Patches the Puzzles static constructor to call CreateSandbox2.
            void PatchPuzzlesConstructor()
            {
                var method = puzzlesType.FindMethod(".cctor");
                var il = method.Body.GetILProcessor();
                il.InsertBefore(method.Body.Instructions.Last(), il.Create(OpCodes.Call, createSandbox2));
            }
        }

        /// <summary>
        /// Adds a new email thread to get to the Sandbox2 puzzle.
        /// </summary>
        private void AddSandbox2MessageThread()
        {
            var messageThreadsType = m_module.FindType("MessageThreads");
            var method = messageThreadsType.FindMethod("#=qwjQuuxUKAcc2rhJ2_3COeg==");
            var il = method.Body.GetILProcessor();

            // Increase the length of the messageThreads array by one
            const int NUM_THREADS = 76;
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)NUM_THREADS).Operand = (sbyte)(NUM_THREADS + 1);

            // We want to insert our new puzzle just after the existing sandbox, so shuffle the
            // later message threads forward by one.
            const int INSERT_INDEX = 18;
            var messageThreadsField = messageThreadsType.FindField("#=qmBNhv6t$m9ggiyIoqso9Ug==");
            var endThreads = method.FindInstructionAtOffset(0x0e95, OpCodes.Call, messageThreadsType.FindMethod("#=qaIFdKxRWR2y8tIK$6OrYcBQLWhmFTT0LwFP$IOxB9$A="));

            // Array.Copy(messageThreadsField, INSERT_INDEX, messageThreadsField, INSERT_INDEX + 1, NUM_THREADS - INSERT_INDEX);
            il.InsertBefore(endThreads,
                il.Create(OpCodes.Ldsfld, messageThreadsField),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)INSERT_INDEX),
                il.Create(OpCodes.Ldsfld, messageThreadsField),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)(INSERT_INDEX + 1)),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)(NUM_THREADS - INSERT_INDEX)),
                il.Create(OpCodes.Call, m_module.ImportReference(typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }))));

            // Now create our new message thread
            // messageThreads[INSERT_INDEX] = CreateMessageThread((Location)0 /* Longteng Co. Ltd. */, /* stage unlocked at */ 6, "bigger-prototyping-area", Puzzles.Sandbox2, L.GetString("Bigger Prototyping Area", ""), (Enum2)2, 4, null);
            il.InsertBefore(endThreads,
                il.Create(OpCodes.Ldsfld, messageThreadsField),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)INSERT_INDEX),
                il.Create(OpCodes.Ldc_I4_0),
                il.Create(OpCodes.Ldc_I4_6),
                il.Create(OpCodes.Ldstr, "bigger-prototyping-area"),    // Name of the messages file containing the email thread (also used to name the solution files on disk)
                il.Create(OpCodes.Ldsfld, m_module.FindField("Puzzles", "Sandbox2")),
                il.Create(OpCodes.Ldstr, "Bigger Prototyping Area"),    // Name of the puzzle shown in the game
                il.Create(OpCodes.Ldstr, ""),
                il.Create(OpCodes.Call, m_module.FindMethod("L", "#=qAr$2Ue4QaJr84iXEbxBSkQ==")),
                il.Create(OpCodes.Ldc_I4_2),
                il.Create(OpCodes.Ldc_I4_4),
                il.Create(OpCodes.Ldnull),
                il.Create(OpCodes.Call, messageThreadsType.FindMethod("#=qfUbtIkU$VEWeJDUjSUPeyd54W9YJOUkOnvfvmUQDBmA=")),
                il.Create(OpCodes.Stelem_Ref));
        }

        /// <summary>
        /// Saves the patched executable to disk.
        /// </summary>
        public void SavePatchedFile(string targetFile)
        {
            sm_log.InfoFormat("Saving patched file to \"{0}\"", targetFile);
            m_module.Write(targetFile);
        }
    }
}
 
 