using System;
using System.Linq;
using Mono.Cecil;
using ShenzhenMod.Patching;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class MessageThreads
    {
        [ResolveByType]
        public static MessageThread[] AllThreads;

        [ResolveByMethod("LocateCreateThread")]
        private static MessageThread CreateThread(
            Location location,
            int unlockStage,
            string name,            // Name of the messages file containing the email thread (also used to name the solution files on disk)
            Puzzle puzzle,
            LocString displayName,  // Name of the puzzle shown in the game
            UnknownEnum enum2,
            int int1,
            string str1)
        {
            return null;
        }

        [ResolveByMethod("LocateCreateAllThreads")]
        public static void CreateAllThreads()
        {
        }

        public static MethodDefinition LocateCreateAllThreads(TypeDefinition type) => type.Methods.Single(m => m.IsPublic && m.IsStatic && m.Body.Variables.Count == 2 && m.Body.Variables[0].VariableType.Name == "MessageThread");
        public static MethodDefinition LocateCreateThread(TypeDefinition type) => type.Methods.Single(m => m.IsPrivate && m.IsStatic && m.Parameters.Count == 8);
    }
}
