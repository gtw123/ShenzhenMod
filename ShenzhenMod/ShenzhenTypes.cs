using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ShenzhenMod
{
    public class ShenzhenTypes
    {
        public class GlobalsType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition ClassConstructor;
            public readonly FieldDefinition MaxBoardSize;

            public GlobalsType(ModuleDefinition module)
            {
                // The Solution constructor has a line like this:
                //     this.someField = new HashSet<int>[Globals.MaxTestRuns];
                // We use this to find the "Globals" type.
                var ctor = module.FindMethod("Solution", ".ctor");
                var instr = ctor.Body.Instructions.Single(i => i.OpCode == OpCodes.Newarr && i.Operand?.ToString() == "System.Collections.Generic.HashSet`1<System.Int32>");
                Type = ((FieldDefinition)instr.Previous.Operand).DeclaringType;
                ClassConstructor = Type.FindMethod(".cctor");

                // Find "MaxBoardSize = new Index2(22, 14)" and use that to get the MaxBoardSize field
                MaxBoardSize = (FieldDefinition)ClassConstructor.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).Next.Next.Operand;
            }
        }

        public class Index2Type
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Constructor;
            public readonly FieldDefinition X;
            public readonly FieldDefinition Y;

            public Index2Type(ModuleDefinition module)
            {
                Type = module.FindType("Index2");
                Constructor = Type.FindMethod(".ctor");
                X = Type.Fields[1];
                Y = Type.Fields[2];
            }
        }

        public class MessageThreadType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Constructor;

            public MessageThreadType(ModuleDefinition module)
            {
                Type = module.FindType("MessageThread");
                Constructor = Type.FindMethod(".ctor");
            }
        }

        public class MessageThreadsType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition ClassConstructor;

            public MessageThreadsType(ModuleDefinition module)
            {
                Type = module.FindType("MessageThreads");
                ClassConstructor = Type.FindMethod(".cctor");
            }
        }

        public class OptionalType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition ClassConstructor;
            public readonly MethodDefinition HasValue;
            public readonly MethodDefinition GetValue;

            public OptionalType(ModuleDefinition module)
            {
                // The Optional<T> type has two fields: a boolean and a T
                Type = module.Types.Single(t => t.IsValueType && t.HasGenericParameters && t.Fields.Count == 2
                    && t.Fields[0].FieldType == module.TypeSystem.Boolean && t.Fields[1].FieldType == t.GenericParameters[0]);
                HasValue = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 0 && m.ReturnType == module.TypeSystem.Boolean);
                GetValue = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 0 && m.ReturnType == Type.GenericParameters[0]);
            }
        }

        public class PuzzleType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Constructor;
            public readonly FieldDefinition Name;
            public readonly FieldDefinition IsSandbox;

            public PuzzleType(ModuleDefinition module)
            {
                Type = module.FindType("Puzzle");
                Constructor = Type.FindMethod(".ctor");
                Name = Type.Fields.First(f => f.FieldType == module.TypeSystem.String);
                IsSandbox = Type.Fields.First(f => f.FieldType == module.TypeSystem.Boolean);
            }
        }

        public class PuzzlesType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition ClassConstructor;
            public readonly MethodDefinition FindPuzzle;

            public PuzzlesType(ModuleDefinition module)
            {
                Type = module.FindType("Puzzles");
                ClassConstructor = Type.FindMethod(".cctor");
                FindPuzzle = Type.Methods.Single(m => m.Parameters.Count == 1 && m.Parameters[0].ParameterType == module.TypeSystem.String);
            }
        }

        public class SolutionType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Constructor;
            public readonly FieldDefinition PuzzleName;

            public SolutionType(ModuleDefinition module)
            {
                Type = module.FindType("Solution");
                Constructor = Type.FindMethod(".ctor");
                PuzzleName = Type.Fields.Where(f => f.FieldType == module.TypeSystem.String).Skip(1).First();
            }
        }

        public class TerminalType
        {
            public readonly TypeDefinition Type;

            public TerminalType(ModuleDefinition module)
            {
                Type = module.FindType("Terminal");
            }
        }

        public readonly ModuleDefinition Module;
        public readonly TypeSystem BuiltIn;

        public readonly GlobalsType Globals;
        public readonly Index2Type Index2;
        public readonly MessageThreadType MessageThread;
        public readonly MessageThreadsType MessageThreads;
        public readonly OptionalType Optional;
        public readonly PuzzleType Puzzle;
        public readonly PuzzlesType Puzzles;
        public readonly SolutionType Solution;
        public readonly TerminalType Terminal;

        public ShenzhenTypes(ModuleDefinition module)
        {
            Module = module;
            BuiltIn = module.TypeSystem;

            Globals = new GlobalsType(module);
            Index2 = new Index2Type(module);
            MessageThread = new MessageThreadType(module);
            MessageThreads = new MessageThreadsType(module);
            Optional = new OptionalType(module);
            Puzzle = new PuzzleType(module);
            Puzzles = new PuzzlesType(module);
            Solution = new SolutionType(module);
            Terminal = new TerminalType(module);
        }
    }
}