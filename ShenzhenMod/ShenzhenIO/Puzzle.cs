using System;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class Puzzle
    {
        [ResolveByPositionAndType]
        public string Name;

        [ResolveByPositionAndType]
        public bool IsSandbox;

        [ResolveByType]
        public Terminal[] Terminals;

        [ResolveByType]
        public int[] Tiles;

        [ResolveByType]
        public Func<RandomGenerator, int, PuzzleOutputs> GetTestRunOutputs;
    }
}