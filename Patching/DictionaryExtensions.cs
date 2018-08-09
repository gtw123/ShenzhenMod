using System.Collections.Generic;

namespace ShenzhenMod.Patching
{
    public static class DictionaryExtensions
    {
        public static U GetValueOrNull<T, U>(this Dictionary<T, U> dictionary, T key) where U : class
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }
    }
}