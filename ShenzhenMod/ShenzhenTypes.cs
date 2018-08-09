using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ShenzhenMod.Patching;

namespace ShenzhenMod
{
    public class ShenzhenTypes
    {
        public class CircuitEditorScreenType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Update;

            public CircuitEditorScreenType(ModuleDefinition module)
            {
                Type = module.FindType("GameLogic/CircuitEditorScreen");

                // The Update() method is also defined on the IScreen interface, so get the name from there
                var updateMethod = module.FindType("IScreen").Methods.Single(m => m.Parameters.Count == 1 && m.Parameters[0].ParameterType == module.TypeSystem.Single);
                Update = Type.FindMethod(updateMethod.Name);
            }
        }

        public class GameLogicType
        {
            public readonly TypeDefinition Type;
            public readonly CircuitEditorScreenType CircuitEditorScreen;

            public GameLogicType(ModuleDefinition module)
            {
                Type = module.FindType("GameLogic");
                CircuitEditorScreen = new CircuitEditorScreenType(module);
            }
        }

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
                MaxBoardSize = (FieldDefinition)ClassConstructor.FindInstruction(OpCodes.Ldc_I4_S, (sbyte)14).FindNext(OpCodes.Stsfld).Operand;
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

        public class LType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition GetString;

            public LType(ModuleDefinition module)
            {
                Type = module.FindType("L");
                GetString = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 2
                    && m.Parameters.All(p => p.ParameterType == module.TypeSystem.String)
                    && m.ReturnType == module.FindType("LocString"));
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
            public readonly MethodDefinition CreateAllThreads;
            public readonly MethodDefinition CreateThread;
            public readonly FieldDefinition AllThreads;

            public MessageThreadsType(ModuleDefinition module)
            {
                Type = module.FindType("MessageThreads");
                ClassConstructor = Type.FindMethod(".cctor");
                CreateAllThreads = Type.Methods.Single(m => m.IsPublic && m.IsStatic && m.Body.Variables.Count == 2
                    && m.Body.Variables[0].VariableType == module.FindType("MessageThread"));
                CreateThread = Type.Methods.Single(m => m.IsPrivate && m.IsStatic && m.Parameters.Count == 8);
                AllThreads = Type.Fields.Single(f => f.FieldType.IsArray && f.FieldType.GetElementType() == module.FindType("MessageThread"));
            }
        }

        public class OptionalType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition ClassConstructor;

            private readonly MethodDefinition m_hasValue;
            private readonly MethodDefinition m_getValue;

            public MethodReference HasValue(TypeReference typeParam)
            {
                return m_hasValue.MakeGenericInstance(typeParam);
            }

            public MethodReference GetValue(TypeReference typeParam)
            {
                return m_getValue.MakeGenericInstance(typeParam);
            }

            public OptionalType(ModuleDefinition module)
            {
                // The Optional<T> type has two fields: a boolean and a T
                Type = module.Types.Single(t => t.IsValueType && t.HasGenericParameters && t.Fields.Count == 2
                    && t.Fields[0].FieldType == module.TypeSystem.Boolean && t.Fields[1].FieldType == t.GenericParameters[0]);
                m_hasValue = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 0 && m.ReturnType == module.TypeSystem.Boolean);
                m_getValue = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 0 && m.ReturnType == Type.GenericParameters[0]);
            }
        }

        public class PuzzleType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition Constructor;
            public readonly FieldDefinition Name;
            public readonly FieldDefinition IsSandbox;
            public readonly FieldDefinition Terminals;
            public readonly FieldDefinition Tiles;
            public readonly FieldDefinition GetTestRunOutputs;

            public PuzzleType(ModuleDefinition module)
            {
                Type = module.FindType("Puzzle");
                Constructor = Type.FindMethod(".ctor");
                Name = Type.Fields.First(f => f.FieldType == module.TypeSystem.String);
                IsSandbox = Type.Fields.First(f => f.FieldType == module.TypeSystem.Boolean);
                Terminals = Type.Fields.Single(f => f.FieldType.ToString() == "Terminal[]");
                Tiles = Type.Fields.Single(f => f.FieldType.ToString() == "System.Int32[]");
                GetTestRunOutputs = Type.Fields.Single(f => f.FieldType.ToString().StartsWith("System.Func`3"));

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
            public readonly FieldDefinition Traces;

            public SolutionType(ModuleDefinition module, TracesType traces)
            {
                Type = module.FindType("Solution");
                Constructor = Type.FindMethod(".ctor");
                PuzzleName = Type.Fields.Where(f => f.FieldType == module.TypeSystem.String).Skip(1).First();
                Traces = Type.Fields.Single(f => f.FieldType == traces.Type);
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

        public class TextureManagerType
        {
            public readonly TypeDefinition Type;

            public TextureManagerType(ModuleDefinition module)
            {
                var textureType = module.FindType("Texture");
                Type = module.Types.Single(t => t.Fields.Count > 24 && t.Fields.Take(10).All(f => f.FieldType == textureType));
            }
        }

        public class TracesType
        {
            public readonly TypeDefinition Type;
            public readonly MethodDefinition GetSize;

            public TracesType(ModuleDefinition module)
            {
                var structType = module.Types.Single(t => t.IsValueType && t.Fields.Count == 2
                    && t.Fields[0].FieldType == module.FindType("Index2")
                    && t.Fields[1].FieldType == module.FindType("Trace"));

                // Traces is an IEnumerable of the above struct type
                Type = module.Types.Single(t => t.Interfaces.Count > 0 && t.Interfaces.Any(i => i.InterfaceType.ToString() == $"System.Collections.Generic.IEnumerable`1<{structType.Name}>"));
                GetSize = Type.Methods.Single(m => m.IsPublic && m.Parameters.Count == 0 && m.ReturnType == module.FindType("Index2"));
            }
        }

        public readonly ModuleDefinition Module;
        public readonly TypeSystem BuiltIn;

        public readonly GameLogicType GameLogic;
        public readonly GlobalsType Globals;
        public readonly Index2Type Index2;
        public readonly LType L;
        public readonly MessageThreadType MessageThread;
        public readonly MessageThreadsType MessageThreads;
        public readonly OptionalType Optional;
        public readonly PuzzleType Puzzle;
        public readonly PuzzlesType Puzzles;
        public readonly SolutionType Solution;
        public readonly TerminalType Terminal;
        public readonly TextureManagerType TextureManager;
        public readonly TracesType Traces;

        public ShenzhenTypes(ModuleDefinition module)
        {
            Module = module;
            BuiltIn = module.TypeSystem;

            GameLogic = new GameLogicType(module);
            Globals = new GlobalsType(module);
            Index2 = new Index2Type(module);
            L = new LType(module);
            MessageThread = new MessageThreadType(module);
            MessageThreads = new MessageThreadsType(module);
            Optional = new OptionalType(module);
            Puzzle = new PuzzleType(module);
            Puzzles = new PuzzlesType(module);
            Terminal = new TerminalType(module);
            TextureManager = new TextureManagerType(module);
            Traces = new TracesType(module);

            Solution = new SolutionType(module, Traces);
        }
    }
}