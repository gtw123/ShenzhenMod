using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public struct Index2
    {
        [ResolveByPositionAndType]
        public int X;

        [ResolveByPositionAndType]
        public int Y;
    }
}
