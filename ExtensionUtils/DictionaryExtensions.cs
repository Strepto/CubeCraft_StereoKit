#if NET6_0_OR_GREATER

#else
using System;
using System.Collections.Generic;
#endif

namespace StereoKitApp.ExtensionUtils
{
    public static class DictionaryExtensions
    {
#if NET6_0_OR_GREATER

#else
        // Semi copied from dotnet 6 source.
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key
        )
        {
            return dictionary.GetValueOrDefault(key, default!);
        }

        // Semi copied from dotnet 6 source.
        public static TValue GetValueOrDefault<TKey, TValue>(
            this IReadOnlyDictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue
        )
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }
#endif
    }
}
