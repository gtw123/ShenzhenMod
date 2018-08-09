using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class L
    {
        [ResolveBySignature]
        public static LocString GetString(string s1, string s2)
        {
            return null;
        }
    }
}
