using System.Collections.Generic;

namespace Asterism {

internal static class StringEnumerableExtension {
    public static string Join(this IEnumerable<string> list, string separator) {
        return string.Join(separator, list);
    }
}

}