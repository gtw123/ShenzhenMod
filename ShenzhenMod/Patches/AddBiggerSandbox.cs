using System;
using System.IO;
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

        private ShenzhenTypes m_types;
        private string m_shenzhenDir;

        public AddBiggerSandbox(ShenzhenTypes types, string shenzhenDir)
        {
            m_types = types;
            m_shenzhenDir = shenzhenDir;
        }

        public void Apply()
        {
            sm_log.Info("Applying patch");

            AddSandbox2Puzzle();
            AddSandbox2MessageThread();
            CopyMessagesFile();
        }

        /// <summary>
        /// Adds the Sandbox2 puzzle to the Puzzles class.
        /// </summary>
        private void AddSandbox2Puzzle()
        {
            const int WIDTH = 44;
            const int HEIGHT = 28;

            // Add the static field for the new puzzle
            var sandbox2Field = new FieldDefinition("Sandbox2", FieldAttributes.Static | FieldAttributes.InitOnly, m_types.Puzzle.Type);
            m_types.Puzzles.Type.Fields.Add(sandbox2Field);

            // Add the method that creates the puzzle
            var createSandbox2 = AddCreateSandbox2();
            PatchPuzzlesConstructor();

            MethodDefinition AddCreateSandbox2()
            {
                var method = new MethodDefinition("CreateSandbox2", MethodAttributes.Private | MethodAttributes.Static, m_types.BuiltIn.Void);
                m_types.Puzzles.Type.Methods.Add(method);
                var il = method.Body.GetILProcessor();

                var puzzleType = m_types.Puzzle.Type;
                var puzzle = new VariableDefinition(puzzleType);
                method.Body.Variables.Add(puzzle);

                // Puzzle puzzle = new Puzzle();
                il.Emit(OpCodes.Newobj, m_types.Puzzle.Constructor);
                il.Emit(OpCodes.Stloc_S, puzzle);

                // puzzle.Name = "SzSandbox2";
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldstr, "SzSandbox2");
                il.Emit(OpCodes.Stfld, m_types.Puzzle.Name);

                // puzzle.IsSandbox = true;
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stfld, m_types.Puzzle.IsSandbox);

                // Find the built-in sandbox. It's the only puzzle with zero terminals.
                var instr = m_types.Puzzles.ClassConstructor.Body.Instructions.Single(i => i.Matches(OpCodes.Ldc_I4_0, null) && i.Next.Matches(OpCodes.Newarr, m_types.Terminal.Type));
                instr = instr.FindNext(OpCodes.Stsfld);
                var sandboxField = (FieldDefinition)instr.Operand;

                // puzzle.GetTestRunOutputs = Puzzles.Sandbox.GetTestRunOutputs;
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldsfld, sandboxField);
                il.Emit(OpCodes.Ldfld, m_types.Puzzle.GetTestRunOutputs);
                il.Emit(OpCodes.Stfld, m_types.Puzzle.GetTestRunOutputs);

                // puzzle.Terminals = new Terminal[0];
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Newarr, m_types.Terminal.Type);
                il.Emit(OpCodes.Stfld, m_types.Puzzle.Terminals);

                // puzzle.SetSize(new Index2(WIDTH, HEIGHT));
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)WIDTH);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)HEIGHT);
                il.Emit(OpCodes.Newobj, m_types.Index2.Constructor);
                il.Emit(OpCodes.Call, puzzleType.FindMethod("SetSize"));

                // puzzle.Tiles = new int[WIDTH * HEIGHT * 3];
                il.Emit(OpCodes.Ldloc_S, puzzle);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)WIDTH);
                il.Emit(OpCodes.Ldc_I4_S, (sbyte)HEIGHT);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Ldc_I4_3);
                il.Emit(OpCodes.Mul);
                il.Emit(OpCodes.Newarr, m_types.BuiltIn.Int32);
                il.Emit(OpCodes.Stfld, m_types.Puzzle.Tiles);

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
                    il.Emit(OpCodes.Ldfld, m_types.Puzzle.Tiles);
                    il.Emit(OpCodes.Ldc_I4, index);
                    il.Emit(OpCodes.Ldc_I4_S, (sbyte)value1);
                    il.Emit(OpCodes.Stelem_I4);

                    // puzzle.Tiles[index + 1] = value2;
                    il.Emit(OpCodes.Ldloc_S, puzzle);
                    il.Emit(OpCodes.Ldfld, m_types.Puzzle.Tiles);
                    il.Emit(OpCodes.Ldc_I4, index + 1);
                    il.Emit(OpCodes.Ldc_I4_S, (sbyte)value2);
                    il.Emit(OpCodes.Stelem_I4);
                }
            }

            // Patches the Puzzles static constructor to call CreateSandbox2.
            void PatchPuzzlesConstructor()
            {
                var method = m_types.Puzzles.ClassConstructor;
                var il = method.Body.GetILProcessor();
                il.InsertBefore(method.Body.Instructions.Last(), il.Create(OpCodes.Call, createSandbox2));
            }
        }

        /// <summary>
        /// Adds a new email thread to get to the Sandbox2 puzzle.
        /// </summary>
        private void AddSandbox2MessageThread()
        {
            var method = m_types.MessageThreads.CreateAllThreads;
            var il = method.Body.GetILProcessor();

            // Increase the length of the messageThreads array by one
            const int NUM_THREADS = 76;
            method.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)NUM_THREADS).Operand = (sbyte)(NUM_THREADS + 1);

            // We want to insert our new puzzle just after the existing sandbox, so shuffle the
            // later message threads forward by one.
            const int INSERT_INDEX = 18;
            var endThreads = method.FindInstruction(OpCodes.Stsfld, m_types.MessageThreads.AllThreads).Next;

            // Array.Copy(AllThreads, INSERT_INDEX, AllThreads, INSERT_INDEX + 1, NUM_THREADS - INSERT_INDEX);
            il.InsertBefore(endThreads,
                il.Create(OpCodes.Ldsfld, m_types.MessageThreads.AllThreads),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)INSERT_INDEX),
                il.Create(OpCodes.Ldsfld, m_types.MessageThreads.AllThreads),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)(INSERT_INDEX + 1)),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)(NUM_THREADS - INSERT_INDEX)),
                il.Create(OpCodes.Call, m_types.Module.ImportReference(typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(int), typeof(Array), typeof(int), typeof(int) }))));

            // Now create our new message thread
            // AllThreads[INSERT_INDEX] = CreateThread((Location)0 /* Longteng Co. Ltd. */, /* stage unlocked at */ 6, "bigger-prototyping-area", Puzzles.Sandbox2, L.GetString("Bigger Prototyping Area", ""), (Enum2)2, 4, null);
            il.InsertBefore(endThreads,
                il.Create(OpCodes.Ldsfld, m_types.MessageThreads.AllThreads),
                il.Create(OpCodes.Ldc_I4_S, (sbyte)INSERT_INDEX),
                il.Create(OpCodes.Ldc_I4_0),
                il.Create(OpCodes.Ldc_I4_6),
                il.Create(OpCodes.Ldstr, "bigger-prototyping-area"),    // Name of the messages file containing the email thread (also used to name the solution files on disk)
                il.Create(OpCodes.Ldsfld, m_types.Puzzles.Type.FindField("Sandbox2")),
                il.Create(OpCodes.Ldstr, "Bigger Prototyping Area"),    // Name of the puzzle shown in the game
                il.Create(OpCodes.Ldstr, ""),
                il.Create(OpCodes.Call, m_types.L.GetString),
                il.Create(OpCodes.Ldc_I4_2),
                il.Create(OpCodes.Ldc_I4_4),
                il.Create(OpCodes.Ldnull),
                il.Create(OpCodes.Call, m_types.MessageThreads.CreateThread),
                il.Create(OpCodes.Stelem_Ref));
        }

        private void CopyMessagesFile()
        {
            using (var stream = System.Reflection.Assembly.GetCallingAssembly().GetManifestResourceStream("ShenzhenMod.Content.messages.en.bigger-prototyping-area.txt"))
            {
                string path = Path.Combine(m_shenzhenDir, "Content", "messages.en", "bigger-prototyping-area.txt");
                using (var file = File.Create(path))
                {
                    sm_log.InfoFormat("Writing resource file to \"{0}\"", path);
                    stream.CopyTo(file);
                }

                // Although we haven't got a Chinese version, we need to have a corresponding file in messages.zh to avoid a crash.
                string path2 = Path.Combine(m_shenzhenDir, "Content", "messages.zh", "bigger-prototyping-area.txt");
                sm_log.InfoFormat("Copying \"{0}\" to \"{1}\"", path, path2);
                File.Copy(path, path2, overwrite: true);
            }
        }
    }
}
 
 