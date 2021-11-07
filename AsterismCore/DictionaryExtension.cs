using System.Collections.Generic;

namespace AsterismCore {

public static class DictionaryExtension {
    public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : class {
        if (dictionary.TryGetValue(key, out var value)) {
            return value;
        }
        return null;
    }
}

}