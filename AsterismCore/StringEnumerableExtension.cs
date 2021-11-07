using System.Collections.Generic;

namespace AsterismCore {

public static class StringEnumerableExtension {
    public static string Join(this IEnumerable<string> list, string separator) {
        return string.Join(separator, list);
    }
}

}