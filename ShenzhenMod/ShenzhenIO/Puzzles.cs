using ShenzhenMod.Patching;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class Puzzles
    {
        [ResolveBySignature]
        public static Optional<Puzzle> FindPuzzle(string name)
        {
            return default(Optional<Puzzle>);
        }
    }
}
