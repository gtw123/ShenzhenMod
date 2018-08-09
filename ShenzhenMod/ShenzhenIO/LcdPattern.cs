using System;
using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class LcdPattern
    {
        [ResolveBySignature]
        public static Optional<LcdPattern> LoadFromImage(string imagePath, bool allowAnySize)
        {
            return new Optional<LcdPattern>();
        }
    }
}
