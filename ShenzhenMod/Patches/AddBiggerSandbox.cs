using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod.Patches
{
    /// <summary>
    /// Adds a bigger sandbox to the game.
    /// </summary>
    public class AddBiggerSandbox
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(AddBiggerSandbox));

        private ModuleDefinition m_module;

        public AddBiggerSandbox(ModuleDefinition module)
        {
            m_module = module;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");
            AddSandbox2Puzzle();
            AddSandbox2MessageThread();
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
    }
}
 
 