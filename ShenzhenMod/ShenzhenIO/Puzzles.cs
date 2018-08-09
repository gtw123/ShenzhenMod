using ShenzhenMod.Patching;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class Puzzles
    {
        [Injected]
        public static readonly Puzzle Sandbox2;

        [ResolveBySignature]
        public static Optional<Puzzle> FindPuzzle(string name)
        {
            return default(Optional<Puzzle>);
        }
    }
}
