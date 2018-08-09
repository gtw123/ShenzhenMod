using ShenzhenMod.Patching.Attributes;

namespace ShenzhenIO
{
    [ResolveByName]
    public class MessageThread
    {
        [ResolveByType]
        public string Name;
    }
}
